using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lite.Procedures.Pipeline.SourceGenerators
{
    internal sealed class ProcedureInfo
    {
        public ProcedureInfo(INamedTypeSymbol procedure, ITypeSymbol args, ITypeSymbol result, bool isAsync, List<INamedTypeSymbol> attributes)
        {
            Procedure = procedure;
            Args = args;
            Result = result;
            IsAsync = isAsync;
            Attributes = attributes;
        }

        public INamedTypeSymbol Procedure { get; }
        public ITypeSymbol Args { get; }
        public ITypeSymbol Result { get; }
        public bool IsAsync { get; }
        public List<INamedTypeSymbol> Attributes { get; }
    }

    /// <summary>
    /// Result of examining one candidate class: either a usable <see cref="ProcedureInfo"/>, or zero
    /// or more diagnostics explaining why it was skipped / degraded (never both silently).
    /// </summary>
    internal readonly struct ExtractionResult
    {
        public ExtractionResult(ProcedureInfo? info, ImmutableArray<Diagnostic> diagnostics)
        {
            Info = info;
            Diagnostics = diagnostics;
        }

        public ProcedureInfo? Info { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        // NOTE: `default(ExtractionResult)` leaves Diagnostics as an uninitialized ImmutableArray
        // (IsDefault == true) — iterating it throws NullReferenceException. Always return this
        // instead of a bare `default` when skipping without a diagnostic to report.
        public static readonly ExtractionResult None = new ExtractionResult(null, ImmutableArray<Diagnostic>.Empty);
    }

    internal static class ProcedureInfoExtractor
    {
        public static ExtractionResult TryExtract(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            var node = (ClassDeclarationSyntax)ctx.Node;
            if (ctx.SemanticModel.GetDeclaredSymbol(node, ct) is not INamedTypeSymbol symbol)
                return ExtractionResult.None;

            if (symbol.IsAbstract || symbol.IsStatic || symbol.TypeKind != TypeKind.Class)
                return ExtractionResult.None;

            ITypeSymbol? argsType = null;
            ITypeSymbol? resultType = null;
            bool? isAsync = null;

            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.TypeArguments.Length != 2) continue;
                var def = iface.OriginalDefinition;
                if (def.ContainingNamespace.ToDisplayString() != "Lite.Procedures") continue;

                if (def.Name == "IAsyncProcedure")
                {
                    argsType = iface.TypeArguments[0];
                    resultType = iface.TypeArguments[1];
                    isAsync = true;
                    break;
                }

                if (def.Name == "IProcedure")
                {
                    argsType = iface.TypeArguments[0];
                    resultType = iface.TypeArguments[1];
                    isAsync = false;
                    break;
                }
            }

            // Matches the broad syntax predicate (any non-abstract class) but isn't one of our
            // procedure interfaces at all — not relevant, nothing to diagnose.
            if (argsType == null || resultType == null || isAsync == null) return ExtractionResult.None;

            // Only diagnose generic-ness / visibility for CONFIRMED procedures, past this point —
            // otherwise every unrelated generic class in the compilation would get a false warning.
            if (symbol.IsGenericType)
            {
                return new ExtractionResult(null, ImmutableArray.Create(Diagnostic.Create(
                    GeneratorDiagnostics.ProcedureSkipped,
                    node.Identifier.GetLocation(),
                    symbol.Name,
                    "it is an open generic type")));
            }

            // The generated factory/pipeline are top-level types in the same assembly. They can only
            // reference the procedure, its argument/result types, and its interceptors if those are
            // visible at assembly scope (public/internal). Skip anything narrower (e.g. private nested
            // test fixtures) — such procedures fall back to the reflection bridge at runtime.
            if (!IsAssemblyVisible(symbol) || !IsAssemblyVisible(argsType) || !IsAssemblyVisible(resultType))
            {
                return new ExtractionResult(null, ImmutableArray.Create(Diagnostic.Create(
                    GeneratorDiagnostics.ProcedureSkipped,
                    node.Identifier.GetLocation(),
                    symbol.Name,
                    "it (or its argument/result type) is not visible outside its declaring assembly")));
            }

            var (attrs, interceptorDiagnostics) = ExtractInterceptorAttributes(symbol);
            return new ExtractionResult(
                new ProcedureInfo(symbol, argsType, resultType, isAsync.Value, attrs),
                interceptorDiagnostics);
        }

        public static bool IsAssemblyVisible(ITypeSymbol? type)
        {
            switch (type)
            {
                case null:
                    return false;
                case IArrayTypeSymbol array:
                    return IsAssemblyVisible(array.ElementType);
                case INamedTypeSymbol named:
                    for (var current = named; current != null; current = current.ContainingType)
                    {
                        switch (current.DeclaredAccessibility)
                        {
                            case Accessibility.Public:
                            case Accessibility.Internal:
                            case Accessibility.ProtectedOrInternal:
                                break;
                            default:
                                return false;
                        }
                    }

                    foreach (var argument in named.TypeArguments)
                        if (!IsAssemblyVisible(argument))
                            return false;

                    return true;
                default:
                    // Type parameters, pointers, etc. — generic procedures are already excluded upstream.
                    return true;
            }
        }

        // A fast path requires every interceptor in the chain to be referenceable by name. If any
        // isn't, drop the whole attribute set so the caller emits a fallback-only factory (still
        // AoT-safe/reflection-free — just not the unrolled FastPipeline) and surface WHY via a
        // diagnostic instead of failing silently.
        private static (List<INamedTypeSymbol> Attributes, ImmutableArray<Diagnostic> Diagnostics) ExtractInterceptorAttributes(
            INamedTypeSymbol procedure)
        {
            var result = new List<INamedTypeSymbol>();
            var invisible = new List<(INamedTypeSymbol Interceptor, Location Location)>();

            foreach (var attr in procedure.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null) continue;

                INamedTypeSymbol? interceptor = null;

                // Generic [InterceptWithAttribute<T>] lives in Lite.Procedures (Lite.Procedures.DotNet
                // package): take T from attrClass.TypeArguments. Namespace is checked (not just the name) so a
                // coincidentally-same-named attribute elsewhere can't be misread as our chain marker.
                if (attrClass.IsGenericType
                    && attrClass.OriginalDefinition.Name == "InterceptWithAttribute"
                    && attrClass.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "Lite.Procedures"
                    && attrClass.TypeArguments.Length == 1
                    && attrClass.TypeArguments[0] is INamedTypeSymbol genArg)
                {
                    interceptor = genArg;
                }
                // Non-generic [InterceptWithAttribute(typeof(X))] lives in Lite.Procedures.Interception
                // (Lite.Procedures.Contracts package): take ctor arg. Same namespace-check reasoning as above.
                else if (!attrClass.IsGenericType
                    && attrClass.Name == "InterceptWithAttribute"
                    && attrClass.ContainingNamespace?.ToDisplayString() == "Lite.Procedures.Interception"
                    && attr.ConstructorArguments.Length == 1
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol typeofArg)
                {
                    interceptor = typeofArg;
                }

                if (interceptor == null) continue;

                result.Add(interceptor);
                if (!IsAssemblyVisible(interceptor))
                {
                    var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
                    invisible.Add((interceptor, location));
                }
            }

            if (invisible.Count == 0)
                return (result, ImmutableArray<Diagnostic>.Empty);

            result.Clear();
            var diagnostics = invisible
                .Select(i => Diagnostic.Create(
                    GeneratorDiagnostics.InterceptorNotVisible, i.Location, i.Interceptor.Name, procedure.Name))
                .ToImmutableArray();
            return (result, diagnostics);
        }
    }
}

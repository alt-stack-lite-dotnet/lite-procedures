using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lite.Procedures.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class PipelineGenerator : IIncrementalGenerator
    {
        private const string NamespaceGenerated = "Lite.Procedures.Generated";
        private const string RegistryClassName = "GeneratedPipelineRegistry";

        private static readonly SymbolDisplayFormat TypeFormat = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationProvider = context.CompilationProvider;
            var output = compilationProvider.Select(static (c, _) => Generate(c));
            context.RegisterSourceOutput(output, (spc, source) =>
            {
                if (source != null)
                    spc.AddSource("GeneratedPipelines.g.cs", SourceText.From(source, Encoding.UTF8));
            });
        }

        private static string? Generate(Compilation compilation)
        {
            var procedureInterface = compilation.GetTypeByMetadataName("Lite.Procedures.IProcedure`2");
            var asyncProcedureInterface = compilation.GetTypeByMetadataName("Lite.Procedures.IAsyncProcedure`2");
            var interceptWithAttribute = compilation.GetTypeByMetadataName("Lite.Procedures.Interceptors.Attributes.InterceptWithAttribute");

            if (procedureInterface == null || asyncProcedureInterface == null || interceptWithAttribute == null)
                return null;

            var proceduresWithChains = new List<(INamedTypeSymbol ProcedureType, bool IsAsync, ITypeSymbol ArgsType, ITypeSymbol ResultType, IReadOnlyList<INamedTypeSymbol> InterceptorTypes)>();

            foreach (var type in GetAllTypes(compilation))
            {
                if (type is not INamedTypeSymbol named || named.IsAbstract || named.IsStatic)
                    continue;

                (bool isAsync, ITypeSymbol? args, ITypeSymbol? result) = ResolveProcedureInterface(named, procedureInterface, asyncProcedureInterface);
                if (args == null || result == null)
                    continue;
                // Skip generic procedure types (type parameters would emit as TArguments/TResult and break the generated class)
                if (args is ITypeParameterSymbol || result is ITypeParameterSymbol)
                    continue;

                var interceptorTypes = GetInterceptWithTypes(named, interceptWithAttribute).ToList();
                proceduresWithChains.Add((named, isAsync, args, result, interceptorTypes));
            }

            if (proceduresWithChains.Count == 0)
                return null;

            var sb = new StringBuilder();
            EmitUsings(sb);

            sb.AppendLine($"namespace {NamespaceGenerated}");
            sb.AppendLine("{");

            foreach (var (procedureType, isAsync, argsType, resultType, interceptorTypes) in proceduresWithChains)
            {
                var argsName = TypeToCSharp(argsType);
                var resultName = TypeToCSharp(resultType);

                if (isAsync)
                    EmitAsyncPipeline(sb, procedureType, argsName, resultName, interceptorTypes);
                else
                    EmitSyncPipeline(sb, procedureType, argsName, resultName, interceptorTypes);
            }

            EmitRegistry(sb, proceduresWithChains);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool IsProcedureInterface(INamedTypeSymbol originalDefinition, string metadataName, string namespaceName)
        {
            if (originalDefinition.MetadataName != metadataName)
                return false;
            var ns = originalDefinition.ContainingNamespace?.ToDisplayString() ?? "";
            return ns == namespaceName;
        }

        private static (bool isAsync, ITypeSymbol? args, ITypeSymbol? result) ResolveProcedureInterface(
            INamedTypeSymbol type,
            INamedTypeSymbol procedureInterface,
            INamedTypeSymbol asyncProcedureInterface)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (!iface.IsGenericType || iface.TypeArguments.Length != 2)
                    continue;
                var orig = iface.OriginalDefinition;
                if (IsProcedureInterface(orig, "IProcedure`2", "Lite.Procedures"))
                {
                    return (false, iface.TypeArguments[0], iface.TypeArguments[1]);
                }
                if (IsProcedureInterface(orig, "IAsyncProcedure`2", "Lite.Procedures"))
                {
                    return (true, iface.TypeArguments[0], iface.TypeArguments[1]);
                }
            }
            return (false, null, null);
        }

        private static IEnumerable<INamedTypeSymbol> GetInterceptWithTypes(INamedTypeSymbol type, INamedTypeSymbol interceptWithAttribute)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass == null)
                    continue;
                if (attr.AttributeClass.MetadataName != "InterceptWithAttribute" ||
                    attr.AttributeClass.ContainingNamespace?.ToDisplayString() != "Lite.Procedures.Interceptors.Attributes")
                    continue;
                if (attr.ConstructorArguments.Length == 0)
                    continue;
                var arg = attr.ConstructorArguments[0];
                if (arg.Value is INamedTypeSymbol interceptorType)
                    yield return interceptorType;
            }
        }

        /// <summary>
        /// Gets all named types from the current compilation only (not referenced assemblies),
        /// so we only generate pipelines for procedures defined in the project that uses the generator.
        /// </summary>
        private static IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation)
        {
            foreach (var t in GetNestedTypes(compilation.GlobalNamespace))
                yield return t;
        }

        private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamespaceOrTypeSymbol nsOrType)
        {
            foreach (var member in nsOrType.GetMembers())
            {
                if (member is INamedTypeSymbol named)
                {
                    yield return named;
                    foreach (var nested in GetNestedTypes(named))
                        yield return nested;
                }
                else if (member is INamespaceSymbol childNs)
                {
                    foreach (var x in GetNestedTypes(childNs))
                        yield return x;
                }
            }
        }

        private static void EmitUsings(StringBuilder sb)
        {
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Lite.Procedures;");
            sb.AppendLine("using Lite.Procedures.Interceptors;");
            sb.AppendLine("using Lite.Procedures.Pipeline;");
            sb.AppendLine("using OneOf;");
            sb.AppendLine("using OneOf.Types;");
            sb.AppendLine();
        }

        private static string TypeToCSharp(ITypeSymbol symbol)
        {
            return symbol.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        }

        private static string ProcedureTypeName(INamedTypeSymbol procedureType)
        {
            var name = procedureType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return name.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "");
        }

        private static string ChainHash(IReadOnlyList<INamedTypeSymbol> chain)
        {
            if (chain.Count == 0) return "Empty";
            var combined = string.Join("_", chain.Select(c => ProcedureTypeName(c)));
            return combined.Length > 60 ? combined.GetHashCode().ToString("X8") : combined;
        }

        private static void EmitSyncPipeline(StringBuilder sb, INamedTypeSymbol procedureType, string argsName, string resultName, IReadOnlyList<INamedTypeSymbol> interceptorTypes)
        {
            var className = "GeneratedPipeline_Sync_" + ProcedureTypeName(procedureType) + "_" + ChainHash(interceptorTypes);
            var procedureDisplay = TypeToCSharp(procedureType);
            var procedureInterface = $"IProcedure<{argsName}, {resultName}>";
            sb.AppendLine($"    internal sealed class {className} : IProcedurePipeline<{argsName}, {resultName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {procedureInterface} _procedure;");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.Append("        private readonly IProcedureInterceptorCore _i").Append(i).AppendLine(";");
            sb.AppendLine();
            sb.Append("        public ").Append(className).Append($"({procedureInterface} procedure");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.Append(", IProcedureInterceptorCore i").Append(i);
            sb.AppendLine(")");
            sb.AppendLine("        {");
            sb.AppendLine("            _procedure = procedure;");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.AppendLine($"            _i{i} = i{i};");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public OneOf<{resultName}, Exception> Invoke({argsName} arguments)");
            sb.AppendLine("        {");
            // Before chain
            for (var i = 0; i < interceptorTypes.Count; i++)
            {
                sb.AppendLine($"            var before{i} = _i{i}.InvokeBefore(arguments);");
                sb.AppendLine($"            if (!before{i}.IsT0)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                var r = before{i}.IsT1 ? before{i}.AsT1 : before{i}.AsT2;");
                sb.AppendLine($"                return RunAfter(arguments, {i}, r);");
                sb.AppendLine($"            }}");
            }
            sb.AppendLine($"            var completedCount = {interceptorTypes.Count};");
            sb.AppendLine("            OneOf<" + resultName + ", Exception> resultOrException;");
            sb.AppendLine("            try { resultOrException = _procedure.Invoke(arguments); }");
            sb.AppendLine("            catch (Exception ex) { resultOrException = ex!; }");
            sb.AppendLine("            return RunAfter(arguments, completedCount, resultOrException);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        private OneOf<{resultName}, Exception> RunAfter({argsName} arguments, int completedCount, OneOf<{resultName}, Exception> resultOrException)");
            sb.AppendLine("        {");
            sb.AppendLine("            var r = resultOrException;");
            for (var idx = interceptorTypes.Count - 1; idx >= 0; idx--)
            {
                sb.AppendLine($"            if (completedCount > {idx})");
                sb.AppendLine("            {");
                sb.AppendLine("                try { r = ((IProcedureInterceptor<{argsName}, {resultName}>)_i{idx}).InvokeAfter(arguments, r); }");
                sb.AppendLine("                catch (Exception ex) { return ex!; }");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return r;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void EmitAsyncPipeline(StringBuilder sb, INamedTypeSymbol procedureType, string argsName, string resultName, IReadOnlyList<INamedTypeSymbol> interceptorTypes)
        {
            var className = "GeneratedPipeline_Async_" + ProcedureTypeName(procedureType) + "_" + ChainHash(interceptorTypes);
            var procedureInterface = $"IAsyncProcedure<{argsName}, {resultName}>";
            sb.AppendLine($"    internal sealed class {className} : IAsyncProcedurePipeline<{argsName}, {resultName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {procedureInterface} _procedure;");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.Append("        private readonly IProcedureInterceptorCore _i").Append(i).AppendLine(";");
            sb.AppendLine();
            sb.Append("        public ").Append(className).Append($"({procedureInterface} procedure");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.Append(", IProcedureInterceptorCore i").Append(i);
            sb.AppendLine(")");
            sb.AppendLine("        {");
            sb.AppendLine("            _procedure = procedure;");
            for (var i = 0; i < interceptorTypes.Count; i++)
                sb.AppendLine($"            _i{i} = i{i};");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public async ValueTask<OneOf<{resultName}, Exception>> InvokeAsync({argsName} arguments, CancellationToken cancellationToken)");
            sb.AppendLine("        {");
            for (var i = 0; i < interceptorTypes.Count; i++)
            {
                sb.AppendLine($"            var before{i} = _i{i} is IAsyncProcedureInterceptor<{argsName}, {resultName}> ai{i}");
                sb.AppendLine($"                ? await ai{i}.InvokeBeforeExecutionAsync(arguments, cancellationToken)");
                sb.AppendLine($"                : ((IProcedureInterceptor<{argsName}, {resultName}>)_i{i}).InvokeBefore(arguments);");
                sb.AppendLine($"            if (!before{i}.IsT0)");
                sb.AppendLine($"                return await RunAfterAsync(arguments, {i}, before{i}, cancellationToken);");
            }
            sb.AppendLine($"            var completedCount = {interceptorTypes.Count};");
            sb.AppendLine("            OneOf<" + resultName + ", Exception> resultOrException;");
            sb.AppendLine("            try { resultOrException = await _procedure.InvokeAsync(arguments, cancellationToken); }");
            sb.AppendLine("            catch (Exception ex) { resultOrException = ex!; }");
            sb.AppendLine("            return await RunAfterAsync(arguments, completedCount, resultOrException, cancellationToken);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        private async ValueTask<OneOf<{resultName}, Exception>> RunAfterAsync({argsName} arguments, int completedCount, OneOf<Success, {resultName}, Exception> beforeResult, CancellationToken ct)");
            sb.AppendLine("        {");
            sb.AppendLine($"            OneOf<{resultName}, Exception> r = beforeResult.IsT1 ? beforeResult.AsT1 : (beforeResult.IsT2 ? beforeResult.AsT2 : default);");
            for (var idx = interceptorTypes.Count - 1; idx >= 0; idx--)
            {
                sb.AppendLine($"            if (completedCount > {idx})");
                sb.AppendLine("            {");
                sb.AppendLine("                try {");
                sb.AppendLine($"                    r = _i{idx} is IAsyncProcedureInterceptor<{argsName}, {resultName}> ai{idx}");
                sb.AppendLine($"                        ? await ai{idx}.InvokeAfterExecutionAsync(arguments, r, ct)");
                sb.AppendLine($"                        : ((IProcedureInterceptor<{argsName}, {resultName}>)_i{idx}).InvokeAfter(arguments, r);");
                sb.AppendLine("                } catch (Exception ex) { return ex!; }");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return r;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        private async ValueTask<OneOf<{resultName}, Exception>> RunAfterAsync({argsName} arguments, int completedCount, OneOf<{resultName}, Exception> resultOrException, CancellationToken ct)");
            sb.AppendLine("        {");
            sb.AppendLine("            var r = resultOrException;");
            for (var idx = interceptorTypes.Count - 1; idx >= 0; idx--)
            {
                sb.AppendLine($"            if (completedCount > {idx})");
                sb.AppendLine("            {");
                sb.AppendLine("                try {");
                sb.AppendLine($"                    r = _i{idx} is IAsyncProcedureInterceptor<{argsName}, {resultName}> ai{idx}");
                sb.AppendLine($"                        ? await ai{idx}.InvokeAfterExecutionAsync(arguments, r, ct)");
                sb.AppendLine($"                        : ((IProcedureInterceptor<{argsName}, {resultName}>)_i{idx}).InvokeAfter(arguments, r);");
                sb.AppendLine("                } catch (Exception ex) { return ex!; }");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return r;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void EmitRegistry(StringBuilder sb, List<(INamedTypeSymbol ProcedureType, bool IsAsync, ITypeSymbol ArgsType, ITypeSymbol ResultType, IReadOnlyList<INamedTypeSymbol> InterceptorTypes)> proceduresWithChains)
        {
            sb.AppendLine($"    internal sealed class {RegistryClassName} : IGeneratedPipelineRegistry");
            sb.AppendLine("    {");
            sb.AppendLine("        public Type? GetGeneratedPipelineType(Type procedureType, Type[] interceptorTypes)");
            sb.AppendLine("        {");
            foreach (var (procedureType, isAsync, _, _, interceptorTypes) in proceduresWithChains)
            {
                var procedureDisplay = TypeToCSharp(procedureType);
                var className = (isAsync ? "GeneratedPipeline_Async_" : "GeneratedPipeline_Sync_") + ProcedureTypeName(procedureType) + "_" + ChainHash(interceptorTypes);
                sb.Append($"            if (procedureType == typeof({procedureDisplay}) && interceptorTypes.Length == {interceptorTypes.Count}");
                for (var i = 0; i < interceptorTypes.Count; i++)
                    sb.Append($" && interceptorTypes[{i}] == typeof({TypeToCSharp(interceptorTypes[i])})");
                sb.AppendLine(")");
                sb.AppendLine($"                return typeof({className});");
            }
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
    }
}

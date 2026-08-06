using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lite.Procedures.Pipeline.SourceGenerators
{
    /// <summary>
    /// Orchestrator: finds candidate procedure classes (<see cref="ProcedureInfoExtractor"/>), reports
    /// any skip/degrade diagnostics, and emits the generated factories/FastPipelines/module-initializer
    /// (<see cref="ProcedureCodeEmitter"/>) for the ones that qualified.
    /// </summary>
    [Generator]
    public sealed class ProcedureFactoryGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var results = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is ClassDeclarationSyntax c
                        && c.BaseList is { Types.Count: > 0 }
                        && !c.Modifiers.Any(static m => m.ValueText == "abstract"),
                    transform: static (ctx, ct) => ProcedureInfoExtractor.TryExtract(ctx, ct))
                .Collect();

            context.RegisterSourceOutput(results, Emit);
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<ExtractionResult> results)
        {
            foreach (var result in results)
                foreach (var diagnostic in result.Diagnostics)
                    context.ReportDiagnostic(diagnostic);

            var procedures = results
                .Where(static r => r.Info is not null)
                .Select(static r => r.Info!)
                .GroupBy(static p => p.Procedure, SymbolEqualityComparer.Default)
                .Select(static g => g.First())
                .ToList();

            if (procedures.Count == 0) return;

            context.AddSource("LiteProceduresGenerated.g.cs", ProcedureCodeEmitter.EmitSource(procedures));
        }
    }
}

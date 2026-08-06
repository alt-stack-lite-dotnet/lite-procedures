using Microsoft.CodeAnalysis;

namespace Lite.Procedures.Pipeline.SourceGenerators
{
    internal static class GeneratorDiagnostics
    {
        public static readonly DiagnosticDescriptor ProcedureSkipped = new DiagnosticDescriptor(
            id: "LITE001",
            title: "Procedure skipped by pipeline code generation",
            messageFormat:
                "Procedure '{0}' was skipped by pipeline code generation because {1}; it falls back to the "
                + "reflection-based bridge (PipelineAssemblerBridge: MakeGenericMethod/MakeGenericType) at runtime. "
                + "Call RequireGeneratedFactories() to fail fast instead of a silent runtime fallback",
            category: "LiteProcedures.Codegen",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InterceptorNotVisible = new DiagnosticDescriptor(
            id: "LITE002",
            title: "Interceptor not visible; unrolled FastPipeline not generated",
            messageFormat:
                "Interceptor '{0}' referenced by [InterceptWith] on procedure '{1}' is not visible outside its "
                + "declaring assembly, so the unrolled FastPipeline will not be generated for '{1}'. A standard "
                + "closed-generic pipeline factory is still generated (reflection-free), just not unrolled",
            category: "LiteProcedures.Codegen",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}

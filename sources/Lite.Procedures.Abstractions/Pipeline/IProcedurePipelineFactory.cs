using System;
using System.Collections.Generic;

namespace Lite.Procedures.Pipeline
{
    /// <summary>
    /// Source-generator-emitted typed factory that builds a closed-generic pipeline for a
    /// procedure without runtime <c>MakeGenericMethod</c>. Depends only on
    /// <see cref="IServiceProvider"/> (BCL) — never on a DI package — so generated code that
    /// implements it stays in the procedure's own assembly with no MS.DI reference.
    /// </summary>
    public interface IProcedurePipelineFactory
    {
        /// <summary>Concrete procedure implementation type (e.g. <c>EchoProcedure</c>).</summary>
        Type ProcedureType { get; }

        /// <summary>Procedure contract resolved from DI (e.g. <c>IAsyncProcedure&lt;Req,Res&gt;</c>).</summary>
        Type ProcedureInterfaceType { get; }

        /// <summary>Builds the pipeline, resolving the procedure and interceptors from <paramref name="services"/>.</summary>
        object Assemble(IServiceProvider services, IReadOnlyList<(Type InterceptorType, int Priority)> interceptors);
    }
}

using System;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Configuration
{
    /// <summary>
    /// Top-level registration builder. Each <c>AddLiteProcedures</c> call gets its own builder
    /// scope: shared interceptors added via <see cref="UseInterceptor{TInterceptor}"/> apply to
    /// every procedure registered through THIS scope. Multiple <c>AddLiteProcedures</c> calls
    /// compose — their registrations accumulate independently.
    /// </summary>
    public interface ILiteProceduresBuilder
    {
        /// <summary>Shared interceptor applied to every procedure registered in this scope.</summary>
        ILiteProceduresBuilder UseInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IInterceptorCore;

        /// <summary>Shared interceptor applied to every procedure registered in this scope.</summary>
        ILiteProceduresBuilder UseInterceptor(Type interceptorType, int priority = 0);

        ILiteProceduresBuilder AddProcedure<TProcedure>()
            where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddProcedure<TProcedure>(Action<IProcedureBuilder> configure)
            where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddProcedure(Type procedureType);

        ILiteProceduresBuilder AddProcedure(Type procedureType, Action<IProcedureBuilder> configure);

        /// <summary>Applies an EF-style per-procedure configuration class.</summary>
        ILiteProceduresBuilder ApplyConfiguration<TProcedure>(IProcedureConfiguration<TProcedure> configuration)
            where TProcedure : IProcedureCore;

        /// <summary>
        /// AoT-strict mode: every procedure must have a source-generated factory; the reflection
        /// fallback is disallowed and registration throws if a factory is missing.
        /// </summary>
        ILiteProceduresBuilder RequireGeneratedFactories();
    }
}

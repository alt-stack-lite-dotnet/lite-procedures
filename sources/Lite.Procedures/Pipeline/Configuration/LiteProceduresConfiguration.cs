using System;
using System.Collections.Generic;

namespace Lite.Procedures.Configuration
{
    /// <summary>Immutable result of an <see cref="ILiteProceduresBuilder"/> scope, consumed by the DI layer.</summary>
    internal sealed class LiteProceduresConfiguration
    {
        public LiteProceduresConfiguration(
            IReadOnlyList<ProcedureRegistration> procedures,
            bool requireGeneratedFactories)
        {
            Procedures = procedures;
            RequireGeneratedFactories = requireGeneratedFactories;
        }

        public IReadOnlyList<ProcedureRegistration> Procedures { get; }
        public bool RequireGeneratedFactories { get; }
    }

    internal sealed class ProcedureRegistration
    {
        public ProcedureRegistration(Type procedureType, IReadOnlyList<InterceptorRegistration> interceptors)
        {
            ProcedureType = procedureType;
            Interceptors = interceptors;
        }

        public Type ProcedureType { get; }

        /// <summary>Merged shared + per-procedure + [InterceptWith] interceptors. Priority ordering is done by the assembler.</summary>
        public IReadOnlyList<InterceptorRegistration> Interceptors { get; }
    }

    internal readonly struct InterceptorRegistration
    {
        public InterceptorRegistration(Type interceptorType, int priority, object? instance = null)
        {
            InterceptorType = interceptorType;
            Priority = priority;
            Instance = instance;
        }

        public Type InterceptorType { get; }
        public int Priority { get; }

        /// <summary>Optional pre-built instance (from the instance-based <c>UseInterceptor</c> overload). Null = resolve from DI.</summary>
        public object? Instance { get; }
    }
}

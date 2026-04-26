using System;
using System.Collections.Generic;

namespace Lite.Procedures.Builders
{
    internal sealed class LiteProceduresConfiguration
    {
        public LiteProceduresConfiguration(
            IReadOnlyList<InterceptorEntry> defaultInterceptors,
            IReadOnlyDictionary<string, PresetDefinition> presets,
            IReadOnlyList<ProcedureRegistration> procedures)
        {
            DefaultInterceptors = defaultInterceptors;
            Presets = presets;
            Procedures = procedures;
        }

        public IReadOnlyList<InterceptorEntry> DefaultInterceptors { get; }
        public IReadOnlyDictionary<string, PresetDefinition> Presets { get; }
        public IReadOnlyList<ProcedureRegistration> Procedures { get; }
    }

    internal sealed class ProcedureRegistration
    {
        public ProcedureRegistration(Type procedureType, IReadOnlyList<Type> interceptorTypes)
        {
            ProcedureType = procedureType;
            InterceptorTypes = interceptorTypes;
        }

        public Type ProcedureType { get; }
        public IReadOnlyList<Type> InterceptorTypes { get; }
    }
}

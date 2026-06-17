using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lite.Procedures.Pipeline
{
    /// <summary>
    /// Process-wide registry of typed pipeline factories emitted by source generators. Generators
    /// populate it via <c>[ModuleInitializer]</c> at assembly load time; the DI layer consults it
    /// during registration and prefers a factory over the reflection-based fallback. Lives in the
    /// abstractions assembly (no DI dependency) so generated code never references a DI package.
    /// </summary>
    public static class PipelineFactoryRegistry
    {
        private static readonly ConcurrentDictionary<Type, IProcedurePipelineFactory> _factories
            = new ConcurrentDictionary<Type, IProcedurePipelineFactory>();

        public static void Register(IProcedurePipelineFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[factory.ProcedureType] = factory;
        }

        public static bool TryGet(Type procedureType, out IProcedurePipelineFactory factory)
            => _factories.TryGetValue(procedureType, out factory!);

        public static bool Contains(Type procedureType) => _factories.ContainsKey(procedureType);

        public static IReadOnlyCollection<Type> ProcedureTypes => _factories.Keys.ToArray();

        /// <summary>Removes all registered factories. Intended for tests; do not call in production code.</summary>
        public static void Clear() => _factories.Clear();
    }
}

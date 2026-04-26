using System;
using System.Collections.Generic;
using System.Linq;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Builders
{
    internal sealed class PipelinePresetBuilder : IPipelinePresetBuilder
    {
        private readonly List<InterceptorEntry> _entries = new List<InterceptorEntry>();
        private bool _inheritGlobals;

        public IPipelinePresetBuilder AddInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IProcedureInterceptorCore
        {
            var type = typeof(TInterceptor);
            if (_entries.Any(e => e.Type == type))
                return this;
            _entries.Add(new InterceptorEntry(type, priority));
            return this;
        }

        public IPipelinePresetBuilder InheritGlobals()
        {
            _inheritGlobals = true;
            return this;
        }

        public IPipelinePresetBuilder IgnoreGlobals()
        {
            _inheritGlobals = false;
            return this;
        }

        public PresetDefinition Build() => new PresetDefinition(_entries.ToArray(), _inheritGlobals);
    }

    internal readonly struct InterceptorEntry
    {
        public InterceptorEntry(Type type, int priority)
        {
            Type = type;
            Priority = priority;
        }

        public Type Type { get; }
        public int Priority { get; }
    }

    internal readonly struct PresetDefinition
    {
        public PresetDefinition(IReadOnlyList<InterceptorEntry> entries, bool inheritGlobals)
        {
            Entries = entries;
            InheritGlobals = inheritGlobals;
        }

        public IReadOnlyList<InterceptorEntry> Entries { get; }
        public bool InheritGlobals { get; }
    }
}

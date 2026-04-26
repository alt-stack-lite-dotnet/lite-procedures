using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Interceptors.Attributes;

namespace Lite.Procedures.Builders
{
    internal sealed class LiteProceduresBuilder : ILiteProceduresBuilder
    {
        private readonly List<InterceptorEntry> _defaultInterceptors = new List<InterceptorEntry>();
        private readonly Dictionary<string, PresetDefinition> _presets = new Dictionary<string, PresetDefinition>();
        private readonly List<ProcedureRegistration> _procedures = new List<ProcedureRegistration>();

        public ILiteProceduresBuilder AddDefaultInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IProcedureInterceptorCore
        {
            AddDefaultInterceptorType(typeof(TInterceptor), priority);
            return this;
        }

        public ILiteProceduresBuilder AddDefaultInterceptors(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var interceptorTypes = assembly.GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false }
                             && typeof(IProcedureInterceptorCore).IsAssignableFrom(t));

                foreach (var type in interceptorTypes)
                    AddDefaultInterceptorType(type, priority: 0);
            }

            return this;
        }

        public ILiteProceduresBuilder AddPreset(string name, Action<IPipelinePresetBuilder> configure)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Preset name must be non-empty.", nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            if (_presets.ContainsKey(name))
                throw new InvalidOperationException($"Preset '{name}' is already registered.");

            var builder = new PipelinePresetBuilder();
            configure(builder);
            _presets[name] = builder.Build();
            return this;
        }

        public ILiteProceduresBuilder AddProcedure<TProcedure>()
            where TProcedure : IProcedureCore
        {
            AddProcedureType(typeof(TProcedure), preset: null);
            return this;
        }

        public ILiteProceduresBuilder AddProcedure<TProcedure>(string preset)
            where TProcedure : IProcedureCore
        {
            if (string.IsNullOrEmpty(preset))
                throw new ArgumentException("Preset name must be non-empty.", nameof(preset));
            AddProcedureType(typeof(TProcedure), preset);
            return this;
        }

        public ILiteProceduresBuilder AddProcedures(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var procedureTypes = assembly.GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false }
                             && typeof(IProcedureCore).IsAssignableFrom(t));

                foreach (var type in procedureTypes)
                    AddProcedureType(type, preset: null);
            }

            return this;
        }

        public ILiteProceduresBuilder AddProcedureWithCustomPipeline<TProcedure>(
            Action<ILiteProceduresPipelineBuilder<TProcedure>> configure)
            where TProcedure : IProcedureCore
        {
            var procedureType = typeof(TProcedure);

            var baseInterceptors = ResolveBaseInterceptors(procedureType, preset: null);

            var pipelineBuilder = new LiteProceduresPipelineBuilder<TProcedure>(baseInterceptors);
            configure(pipelineBuilder);

            var interceptorTypes = pipelineBuilder.Build();
            _procedures.Add(new ProcedureRegistration(procedureType, interceptorTypes));
            return this;
        }

        internal LiteProceduresConfiguration Build()
            => new LiteProceduresConfiguration(_defaultInterceptors, _presets, _procedures);

        private void AddDefaultInterceptorType(Type type, int priority)
        {
            if (_defaultInterceptors.Any(e => e.Type == type))
                return;

            _defaultInterceptors.Add(new InterceptorEntry(type, priority));
        }

        private void AddProcedureType(Type type, string? preset)
        {
            if (_procedures.Any(r => r.ProcedureType == type))
                return;

            var interceptorTypes = ResolveBaseInterceptors(type, preset);
            _procedures.Add(new ProcedureRegistration(type, interceptorTypes));
        }

        private Type[] ResolveBaseInterceptors(Type procedureType, string? preset)
        {
            IEnumerable<InterceptorEntry> baseEntries;

            if (preset == null)
            {
                baseEntries = _defaultInterceptors;
            }
            else
            {
                if (!_presets.TryGetValue(preset, out var presetDefinition))
                    throw new InvalidOperationException(
                        $"Preset '{preset}' is not registered. Call AddPreset(\"{preset}\", ...) before adding a procedure to it.");

                baseEntries = presetDefinition.InheritGlobals
                    ? _defaultInterceptors.Concat(presetDefinition.Entries)
                    : presetDefinition.Entries;
            }

            var orderedBase = baseEntries
                .OrderBy(e => e.Priority)
                .Select(e => e.Type);

            var attributeInterceptors = procedureType
                .GetCustomAttributes<InterceptWithAttribute>(inherit: true)
                .Select(a => a.InterceptorType);

            return orderedBase.Concat(attributeInterceptors).ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using Lite.Procedures.Interception;

namespace Lite.Procedures.Configuration
{
    internal sealed class LiteProceduresBuilder : ILiteProceduresBuilder
    {
        private readonly List<InterceptorRegistration> _shared = new List<InterceptorRegistration>();
        private readonly List<(Type ProcedureType, IReadOnlyList<InterceptorRegistration> PerProcedure)> _procedures
            = new List<(Type, IReadOnlyList<InterceptorRegistration>)>();
        private bool _requireGeneratedFactories;

        public ILiteProceduresBuilder UseInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IInterceptorCore
        {
            _shared.Add(new InterceptorRegistration(typeof(TInterceptor), priority));
            return this;
        }

        public ILiteProceduresBuilder UseInterceptor(Type interceptorType, int priority = 0)
        {
            if (interceptorType == null) throw new ArgumentNullException(nameof(interceptorType));
            _shared.Add(new InterceptorRegistration(interceptorType, priority));
            return this;
        }

        public ILiteProceduresBuilder AddProcedure<TProcedure>()
            where TProcedure : IProcedureCore
            => AddProcedureCore(typeof(TProcedure), configure: null);

        public ILiteProceduresBuilder AddProcedure<TProcedure>(Action<IProcedureBuilder> configure)
            where TProcedure : IProcedureCore
            => AddProcedureCore(typeof(TProcedure), configure);

        public ILiteProceduresBuilder AddProcedure(Type procedureType)
            => AddProcedureCore(Validate(procedureType), configure: null);

        public ILiteProceduresBuilder AddProcedure(Type procedureType, Action<IProcedureBuilder> configure)
            => AddProcedureCore(Validate(procedureType), configure);

        public ILiteProceduresBuilder ApplyConfiguration<TProcedure>(IProcedureConfiguration<TProcedure> configuration)
            where TProcedure : IProcedureCore
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            return AddProcedureCore(typeof(TProcedure), configuration.Configure);
        }

        public ILiteProceduresBuilder RequireGeneratedFactories()
        {
            _requireGeneratedFactories = true;
            return this;
        }

        internal LiteProceduresConfiguration Build()
        {
            var procedures = new List<ProcedureRegistration>(_procedures.Count);

            foreach (var (procedureType, perProcedure) in _procedures)
            {
                var merged = new List<InterceptorRegistration>(_shared.Count + perProcedure.Count + 2);
                merged.AddRange(_shared);
                merged.AddRange(perProcedure);

                foreach (var attribute in procedureType.GetCustomAttributes<InterceptWithAttribute>(inherit: true))
                    merged.Add(new InterceptorRegistration(attribute.InterceptorType, priority: 0));

                ThrowIfDuplicateDiResolvedInterceptor(procedureType, merged);

                procedures.Add(new ProcedureRegistration(procedureType, merged));
            }

            return new LiteProceduresConfiguration(procedures, _requireGeneratedFactories);
        }

        // Shared + per-procedure + [InterceptWith] merge without deduplication by design (a type can
        // legitimately appear twice with two distinct explicit instances). But two occurrences that
        // BOTH resolve from DI (no explicit instance) would run the exact same singleton object twice
        // in the pipeline — that's never intentional, so fail fast instead of silently double-invoking.
        private static void ThrowIfDuplicateDiResolvedInterceptor(Type procedureType, List<InterceptorRegistration> merged)
        {
            Dictionary<Type, int>? counts = null;
            foreach (var registration in merged)
            {
                if (registration.Instance != null) continue;

                counts ??= new Dictionary<Type, int>();
                counts.TryGetValue(registration.InterceptorType, out var count);
                counts[registration.InterceptorType] = count + 1;
            }

            if (counts == null) return;

            foreach (var (interceptorType, count) in counts)
            {
                if (count <= 1) continue;
                throw new InvalidOperationException(
                    $"Interceptor '{interceptorType.FullName}' is registered {count} times for procedure " +
                    $"'{procedureType.FullName}' without an explicit instance (e.g. via shared UseInterceptor<T>() " +
                    $"and [InterceptWith(typeof(T))] together) — it would run {count} times in the same pipeline. " +
                    $"Remove the duplicate registration, or use UseInterceptor(instance) with distinct instances " +
                    $"if running it more than once is intentional.");
            }
        }

        // AddProcedure<T>() called more than once in the same scope MERGES interceptors from every
        // call into a single entry — it does not throw, and does not keep two separate entries for
        // the same type. Two entries would make Build()'s [InterceptWith] attribute scan run twice for
        // that type, doubling the attribute-declared interceptors and breaking the generated
        // FastPipeline's exact interceptor-count match. Merging here (before Build() ever runs) keeps
        // exactly one entry per procedure type, so that scan — and the fast-path match — stays correct.
        private ILiteProceduresBuilder AddProcedureCore(Type procedureType, Action<IProcedureBuilder>? configure)
        {
            IReadOnlyList<InterceptorRegistration> newInterceptors = Array.Empty<InterceptorRegistration>();
            if (configure != null)
            {
                var procedureBuilder = new ProcedureBuilder();
                configure(procedureBuilder);
                newInterceptors = procedureBuilder.Interceptors;
            }

            var existingIndex = _procedures.FindIndex(p => p.ProcedureType == procedureType);
            if (existingIndex < 0)
            {
                _procedures.Add((procedureType, newInterceptors));
                return this;
            }

            var existingInterceptors = _procedures[existingIndex].PerProcedure;
            var merged = new List<InterceptorRegistration>(existingInterceptors.Count + newInterceptors.Count);
            merged.AddRange(existingInterceptors);
            merged.AddRange(newInterceptors);
            _procedures[existingIndex] = (procedureType, merged);
            return this;
        }

        private static Type Validate(Type procedureType)
        {
            if (procedureType == null) throw new ArgumentNullException(nameof(procedureType));
            if (!typeof(IProcedureCore).IsAssignableFrom(procedureType))
                throw new ArgumentException(
                    $"Type '{procedureType.FullName}' does not implement IProcedureCore.",
                    nameof(procedureType));
            return procedureType;
        }
    }
}

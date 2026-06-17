using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Pipeline.Interception;

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

                procedures.Add(new ProcedureRegistration(procedureType, merged));
            }

            return new LiteProceduresConfiguration(procedures, _requireGeneratedFactories);
        }

        private ILiteProceduresBuilder AddProcedureCore(Type procedureType, Action<IProcedureBuilder>? configure)
        {
            if (_procedures.Any(p => p.ProcedureType == procedureType))
                return this;

            IReadOnlyList<InterceptorRegistration> perProcedure = Array.Empty<InterceptorRegistration>();
            if (configure != null)
            {
                var procedureBuilder = new ProcedureBuilder();
                configure(procedureBuilder);
                perProcedure = procedureBuilder.Interceptors;
            }

            _procedures.Add((procedureType, perProcedure));
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

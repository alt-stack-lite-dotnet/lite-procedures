using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Generated;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Interceptors.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lite.Procedures.DependencyInjection
{
    internal sealed class LiteProceduresBuilder : ILiteProceduresBuilder
    {
        private readonly IServiceCollection _services;
        private readonly List<Type> _globalInterceptors = new List<Type>();
        private readonly List<ProcedureRegistration> _procedures = new List<ProcedureRegistration>();

        public LiteProceduresBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public ILiteProceduresBuilder AddGlobalInterceptor<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore
        {
            AddGlobalInterceptorType(typeof(TInterceptor));
            return this;
        }

        public ILiteProceduresBuilder AddGlobalInterceptors(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var interceptorTypes = assembly.GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false }
                             && typeof(IProcedureInterceptorCore).IsAssignableFrom(t));

                foreach (var type in interceptorTypes)
                    AddGlobalInterceptorType(type);
            }

            return this;
        }

        public ILiteProceduresBuilder AddProcedure<TProcedure>()
            where TProcedure : IProcedureCore
        {
            AddProcedureType(typeof(TProcedure));
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
                    AddProcedureType(type);
            }

            return this;
        }

        public ILiteProceduresBuilder AddProcedureWithCustomPipeline<TProcedure>(
            Action<ILiteProceduresPipelineBuilder<TProcedure>> configure)
            where TProcedure : IProcedureCore
        {
            var procedureType = typeof(TProcedure);

            // База: global + атрибуты
            var baseInterceptors = ProcedureRegistration.ResolveInterceptorTypes(procedureType, _globalInterceptors);

            var pipelineBuilder = new LiteProceduresPipelineBuilder<TProcedure>(baseInterceptors);
            configure(pipelineBuilder);

            var interceptorTypes = pipelineBuilder.Build();

            foreach (var interceptorType in interceptorTypes)
                _services.TryAddSingleton(interceptorType);

            _procedures.Add(new ProcedureRegistration(procedureType, interceptorTypes));
            _services.TryAddSingleton(procedureType);
            return this;
        }

        public void Build()
        {
            var generatedRegistry = GeneratedPipelineRegistryDiscovery.TryDiscover();
            foreach (var registration in _procedures)
                registration.Register(_services, generatedRegistry);
        }

        private void AddGlobalInterceptorType(Type type)
        {
            if (_globalInterceptors.Contains(type))
                return;

            _globalInterceptors.Add(type);
            _services.TryAddSingleton(type);
        }

        private void AddProcedureType(Type type)
        {
            if (_procedures.Any(r => r.ProcedureType == type))
                return;

            // Регистрируем интерцепторы из атрибутов
            foreach (var attr in type.GetCustomAttributes<InterceptWithAttribute>(inherit: true))
                _services.TryAddSingleton(attr.InterceptorType);

            var interceptorTypes = ProcedureRegistration.ResolveInterceptorTypes(type, _globalInterceptors);
            _procedures.Add(new ProcedureRegistration(type, interceptorTypes));
            _services.TryAddSingleton(type);
        }
    }
}
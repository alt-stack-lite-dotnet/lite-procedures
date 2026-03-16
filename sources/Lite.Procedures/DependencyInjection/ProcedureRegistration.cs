using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Generated;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Interceptors.Attributes;
using Lite.Procedures.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.DependencyInjection
{
    internal sealed class ProcedureRegistration
    {
        public Type ProcedureType { get; }
        private readonly Type[] _interceptorTypes;

        public ProcedureRegistration(Type procedureType, IReadOnlyList<Type> interceptorTypes)
        {
            ProcedureType = procedureType;
            _interceptorTypes = interceptorTypes.ToArray();
        }

        public void Register(IServiceCollection services, IGeneratedPipelineRegistry? generatedRegistry = null)
        {
            var (argumentsType, resultType, isAsync) = ResolveProcedureTypes(ProcedureType);

            var procedureInterfaceType = isAsync
                ? typeof(IAsyncProcedure<,>).MakeGenericType(argumentsType, resultType)
                : typeof(IProcedure<,>).MakeGenericType(argumentsType, resultType);

            var procedureType = ProcedureType;
            var interceptorTypes = _interceptorTypes;

            services.AddSingleton(procedureInterfaceType, sp =>
            {
                var procedure = sp.GetRequiredService(procedureType);

                var generatedPipelineType = generatedRegistry?.GetGeneratedPipelineType(procedureType, interceptorTypes);
                if (generatedPipelineType != null)
                {
                    var args = new object[1 + interceptorTypes.Length];
                    args[0] = procedure;
                    for (var i = 0; i < interceptorTypes.Length; i++)
                        args[1 + i] = sp.GetRequiredService(interceptorTypes[i]);
                    return Activator.CreateInstance(generatedPipelineType, args)!;
                }

                var pipelineType = isAsync
                    ? typeof(AsyncProcedurePipeline<,>).MakeGenericType(argumentsType, resultType)
                    : typeof(ProcedurePipeline<,>).MakeGenericType(argumentsType, resultType);

                var interceptors = new IProcedureInterceptorCore[interceptorTypes.Length];
                for (var i = 0; i < interceptorTypes.Length; i++)
                    interceptors[i] = (IProcedureInterceptorCore)sp.GetRequiredService(interceptorTypes[i]);

                return Activator.CreateInstance(pipelineType, procedure, interceptors)!;
            });
        }

        public static Type[] ResolveInterceptorTypes(Type procedureType, IReadOnlyList<Type> globalInterceptors)
        {
            var attributeInterceptors = procedureType
                .GetCustomAttributes<InterceptWithAttribute>(inherit: true)
                .Select(a => a.InterceptorType);

            return globalInterceptors
                .Concat(attributeInterceptors)
                .ToArray();
        }

        private static (Type argumentsType, Type resultType, bool isAsync) ResolveProcedureTypes(Type procedureType)
        {
            foreach (var iface in procedureType.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var def = iface.GetGenericTypeDefinition();

                if (def == typeof(IAsyncProcedure<,>))
                {
                    var args = iface.GetGenericArguments();
                    return (args[0], args[1], isAsync: true);
                }

                if (def == typeof(IProcedure<,>))
                {
                    var args = iface.GetGenericArguments();
                    return (args[0], args[1], isAsync: false);
                }
            }

            throw new InvalidOperationException(
                $"Type {procedureType} does not implement IProcedure<,> or IAsyncProcedure<,>.");
        }
    }
}
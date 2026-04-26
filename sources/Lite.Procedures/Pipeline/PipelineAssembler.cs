using System;
using System.Collections.Generic;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Pipeline
{
    public sealed class PipelineAssembler
    {
        public IProcedurePipelineCore Assemble(
            Type procedureType,
            IReadOnlyList<Type> interceptorTypes,
            IServiceProvider services)
        {
            if (procedureType == null) throw new ArgumentNullException(nameof(procedureType));
            if (interceptorTypes == null) throw new ArgumentNullException(nameof(interceptorTypes));
            if (services == null) throw new ArgumentNullException(nameof(services));

            var (argumentsType, resultType, isAsync) = ResolveProcedureSignature(procedureType);

            var procedure = services.GetService(procedureType)
                ?? throw new InvalidOperationException(
                    $"No service of type '{procedureType}' has been registered.");

            return isAsync
                ? AssembleAsync(argumentsType, resultType, procedure, interceptorTypes, services)
                : AssembleSync(argumentsType, resultType, procedure, interceptorTypes, services);
        }

        public static (Type argumentsType, Type resultType, bool isAsync) ResolveProcedureSignature(Type procedureType)
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

        public static Type ResolvePipelineInterface(Type procedureType)
        {
            var (argumentsType, resultType, isAsync) = ResolveProcedureSignature(procedureType);

            return isAsync
                ? typeof(IAsyncProcedurePipeline<,>).MakeGenericType(argumentsType, resultType)
                : typeof(IProcedurePipeline<,>).MakeGenericType(argumentsType, resultType);
        }

        private static IProcedurePipelineCore AssembleSync(
            Type argumentsType,
            Type resultType,
            object procedure,
            IReadOnlyList<Type> interceptorTypes,
            IServiceProvider services)
        {
            var interceptorInterface = typeof(IProcedureInterceptor<,>).MakeGenericType(argumentsType, resultType);
            var asyncInterceptorInterface =
                typeof(IAsyncProcedureInterceptor<,>).MakeGenericType(argumentsType, resultType);

            var interceptors = Array.CreateInstance(interceptorInterface, interceptorTypes.Count);

            for (var i = 0; i < interceptorTypes.Count; i++)
            {
                var interceptorType = interceptorTypes[i];
                var instance = services.GetService(interceptorType)
                    ?? throw new InvalidOperationException(
                        $"No service of type '{interceptorType}' has been registered.");

                if (interceptorInterface.IsInstanceOfType(instance))
                {
                    interceptors.SetValue(instance, i);
                    continue;
                }

                if (asyncInterceptorInterface.IsInstanceOfType(instance))
                {
                    throw new InvalidOperationException(
                        $"Interceptor {interceptorType} is async (IAsyncProcedureInterceptor<{argumentsType.Name}, {resultType.Name}>) " +
                        $"but the procedure is synchronous. Async interceptors can only be used with async procedures.");
                }

                throw new InvalidOperationException(
                    $"Interceptor {interceptorType} does not implement IProcedureInterceptor<{argumentsType.Name}, {resultType.Name}>.");
            }

            var pipelineType = typeof(ProcedurePipeline<,>).MakeGenericType(argumentsType, resultType);
            return (IProcedurePipelineCore)Activator.CreateInstance(pipelineType, procedure, interceptors)!;
        }

        private static IProcedurePipelineCore AssembleAsync(
            Type argumentsType,
            Type resultType,
            object procedure,
            IReadOnlyList<Type> interceptorTypes,
            IServiceProvider services)
        {
            var asyncInterceptorInterface =
                typeof(IAsyncProcedureInterceptor<,>).MakeGenericType(argumentsType, resultType);
            var syncInterceptorInterface =
                typeof(IProcedureInterceptor<,>).MakeGenericType(argumentsType, resultType);

            var interceptors = Array.CreateInstance(asyncInterceptorInterface, interceptorTypes.Count);

            for (var i = 0; i < interceptorTypes.Count; i++)
            {
                var interceptorType = interceptorTypes[i];
                var instance = services.GetService(interceptorType)
                    ?? throw new InvalidOperationException(
                        $"No service of type '{interceptorType}' has been registered.");

                if (asyncInterceptorInterface.IsInstanceOfType(instance))
                {
                    interceptors.SetValue(instance, i);
                    continue;
                }

                if (syncInterceptorInterface.IsInstanceOfType(instance))
                {
                    throw new InvalidOperationException(
                        $"Interceptor {interceptorType} is synchronous (IProcedureInterceptor<{argumentsType.Name}, {resultType.Name}>) " +
                        $"but the procedure is asynchronous. Sync interceptors can only be used with sync procedures.");
                }

                throw new InvalidOperationException(
                    $"Interceptor {interceptorType} does not implement IAsyncProcedureInterceptor<{argumentsType.Name}, {resultType.Name}>.");
            }

            var pipelineType = typeof(AsyncProcedurePipeline<,>).MakeGenericType(argumentsType, resultType);
            return (IProcedurePipelineCore)Activator.CreateInstance(pipelineType, procedure, interceptors)!;
        }
    }
}

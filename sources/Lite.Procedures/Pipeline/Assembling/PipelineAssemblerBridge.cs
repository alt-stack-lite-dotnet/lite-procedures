using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Interception;

namespace Lite.Procedures.Assembling
{
    /// <summary>
    /// Bridge that materializes a typed pipeline (sync or async) from raw <see cref="Type"/>
    /// metadata (procedure type + interceptor types with priorities) and a generic instance
    /// resolver. Used by callers that don't know <c>TArguments</c>/<c>TResult</c> statically —
    /// most notably plugin / no-source-generator scenarios that supply
    /// <c>type =&gt; sp.GetRequiredService(type)</c>.
    /// One <see cref="MethodInfo.MakeGenericMethod"/> call per pipeline registration; nothing on
    /// the hot invocation path. Source-generated paths know the closed types statically and call
    /// <see cref="PipelineAssembler.Assemble{TArguments, TResult}"/> (async) or
    /// <see cref="ProcedurePipeline{TArguments, TResult}"/> (sync) directly, skipping this bridge.
    /// </summary>
    internal static class PipelineAssemblerBridge
    {
        private static readonly MethodInfo GenericAssembleAsyncMethod = typeof(PipelineAssemblerBridge)
            .GetMethod(nameof(AssembleAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo GenericAssembleSyncMethod = typeof(PipelineAssemblerBridge)
            .GetMethod(nameof(AssembleSyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

        internal static object Assemble(
            PipelineAssembler assembler,
            Type procedureType,
            IEnumerable<(Type InterceptorType, int Priority)> interceptors,
            Func<Type, object> resolver)
        {
            if (assembler == null) throw new ArgumentNullException(nameof(assembler));
            if (procedureType == null) throw new ArgumentNullException(nameof(procedureType));
            if (interceptors == null) throw new ArgumentNullException(nameof(interceptors));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            var (isAsync, argsType, resultType) = ResolveProcedureSignature(procedureType);
            var interceptorArray = interceptors.ToArray();

            if (isAsync)
            {
                var dispatcher = GenericAssembleAsyncMethod.MakeGenericMethod(argsType, resultType);
                return dispatcher.Invoke(
                    obj: null,
                    parameters: new object[] { assembler, procedureType, interceptorArray, resolver })!;
            }

            var syncDispatcher = GenericAssembleSyncMethod.MakeGenericMethod(argsType, resultType);
            return syncDispatcher.Invoke(
                obj: null,
                parameters: new object[] { procedureType, interceptorArray, resolver })!;
        }

        internal static (bool IsAsync, Type ArgumentsType, Type ResultType) ResolveProcedureSignature(Type procedureType)
        {
            foreach (var iface in procedureType.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var definition = iface.GetGenericTypeDefinition();
                if (definition == typeof(IAsyncProcedure<,>))
                {
                    var args = iface.GetGenericArguments();
                    return (true, args[0], args[1]);
                }
                if (definition == typeof(IProcedure<,>))
                {
                    var args = iface.GetGenericArguments();
                    return (false, args[0], args[1]);
                }
            }

            throw new InvalidOperationException(
                $"Type {procedureType} does not implement IAsyncProcedure<,> or IProcedure<,>.");
        }

        internal static Type ResolveProcedureInterface(Type procedureType)
        {
            var (isAsync, argsType, resultType) = ResolveProcedureSignature(procedureType);
            return (isAsync ? typeof(IAsyncProcedure<,>) : typeof(IProcedure<,>))
                .MakeGenericType(argsType, resultType);
        }

        private static IAsyncProcedure<TArguments, TResult> AssembleAsyncGeneric<TArguments, TResult>(
            PipelineAssembler assembler,
            Type procedureType,
            (Type InterceptorType, int Priority)[] interceptors,
            Func<Type, object> resolver)
        {
            Func<IAsyncProcedure<TArguments, TResult>> procFactory =
                () => (IAsyncProcedure<TArguments, TResult>)resolver(procedureType);

            var descriptors = interceptors.Select(ir => new AsyncInterceptorDescriptor<TArguments, TResult>(
                ir.Priority,
                () => (AsyncInterceptor<TArguments, TResult>)resolver(ir.InterceptorType)));

            return assembler.Assemble(procFactory, descriptors);
        }

        // Sync has no assembler class to delegate to (see the doc comment above) — the priority
        // ordering and no-wrap-on-empty behaviour that PipelineAssembler.Assemble gives the async
        // path for free is reproduced here by hand.
        private static IProcedure<TArguments, TResult> AssembleSyncGeneric<TArguments, TResult>(
            Type procedureType,
            (Type InterceptorType, int Priority)[] interceptors,
            Func<Type, object> resolver)
        {
            var procedure = (IProcedure<TArguments, TResult>)resolver(procedureType);

            var ordered = interceptors
                .OrderBy(ir => ir.Priority)
                .Select(ir => (Interceptor<TArguments, TResult>)resolver(ir.InterceptorType))
                .ToArray();

            if (ordered.Length == 0) return procedure;

            return new ProcedurePipeline<TArguments, TResult>(procedure, ordered);
        }
    }
}

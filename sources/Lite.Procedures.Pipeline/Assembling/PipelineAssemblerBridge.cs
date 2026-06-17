using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline.Assembling
{
    /// <summary>
    /// Bridge that materializes a typed async pipeline from raw <see cref="Type"/> metadata
    /// (procedure type + interceptor types with priorities) and a generic instance resolver.
    /// Used by callers that don't know <c>TArguments</c>/<c>TResult</c> statically — most
    /// notably plugin / no-source-generator scenarios that supply
    /// <c>type =&gt; sp.GetRequiredService(type)</c>.
    /// One <see cref="MethodInfo.MakeGenericMethod"/> call per pipeline registration; nothing on
    /// the hot invocation path. Source-generated paths know the closed types statically and call
    /// <see cref="PipelineAssembler.Assemble{TArguments, TResult}"/> directly, skipping this bridge.
    /// </summary>
    public static class PipelineAssemblerBridge
    {
        private static readonly MethodInfo GenericAssembleMethod = typeof(PipelineAssemblerBridge)
            .GetMethod(nameof(AssembleGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

        public static object Assemble(
            PipelineAssembler assembler,
            Type procedureType,
            IEnumerable<(Type InterceptorType, int Priority)> interceptors,
            Func<Type, object> resolver)
        {
            if (assembler == null) throw new ArgumentNullException(nameof(assembler));
            if (procedureType == null) throw new ArgumentNullException(nameof(procedureType));
            if (interceptors == null) throw new ArgumentNullException(nameof(interceptors));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            var (argsType, resultType) = ResolveProcedureSignature(procedureType);

            var typedDispatcher = GenericAssembleMethod.MakeGenericMethod(argsType, resultType);
            return typedDispatcher.Invoke(
                obj: null,
                parameters: new object[] { assembler, procedureType, interceptors.ToArray(), resolver })!;
        }

        public static (Type ArgumentsType, Type ResultType) ResolveProcedureSignature(Type procedureType)
        {
            foreach (var iface in procedureType.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() == typeof(IAsyncProcedure<,>))
                {
                    var args = iface.GetGenericArguments();
                    return (args[0], args[1]);
                }
            }

            throw new InvalidOperationException(
                $"Type {procedureType} does not implement IAsyncProcedure<,>.");
        }

        public static Type ResolveProcedureInterface(Type procedureType)
        {
            var (argsType, resultType) = ResolveProcedureSignature(procedureType);
            return typeof(IAsyncProcedure<,>).MakeGenericType(argsType, resultType);
        }

        private static IAsyncProcedure<TArguments, TResult> AssembleGeneric<TArguments, TResult>(
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
    }
}

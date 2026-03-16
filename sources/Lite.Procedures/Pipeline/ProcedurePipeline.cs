using System;
using System.Runtime.CompilerServices;
using Lite.Procedures.Interceptors;
using OneOf;
using OneOf.Types;


namespace Lite.Procedures.Pipeline
{
    public sealed class ProcedurePipeline<TArguments, TResult> : IProcedurePipeline<TArguments, TResult>
    {
        private readonly IProcedure<TArguments, TResult> _procedure;
        private readonly IProcedureInterceptorCore[] _interceptors;

        public ProcedurePipeline(
            IProcedure<TArguments, TResult> procedure,
            IProcedureInterceptorCore[] interceptors)
        {
            _procedure = procedure;
            _interceptors = interceptors;
        }

        public OneOf<TResult, Exception> Invoke(TArguments arguments)
        {
            var (completedCount, beforeChainInvocationResult) = RunBefore(arguments);

            if (!beforeChainInvocationResult.TryPickT0(out _, out var resultOrException))
            {
                return RunAfter(arguments, completedCount, resultOrException);
            }

            resultOrException = InvokeProcedure(arguments);

            return RunAfter(arguments, completedCount, resultOrException);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OneOf<TResult, Exception> InvokeProcedure(TArguments arguments)
        {
            try
            {
                return _procedure.Invoke(arguments);
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private (int completedCount, OneOf<Success, TResult, Exception>) RunBefore(TArguments arguments)
        {
            var completedCount = 0;

            foreach (var interceptor in _interceptors)
            {
                var beforeInvocationResult = interceptor switch
                {
                    IProcedureInterceptor<TArguments, TResult> syncInterceptor =>
                        syncInterceptor.InvokeBefore(arguments),

                    _ => throw new InvalidOperationException(
                        $"Interceptor {interceptor.GetType()} does not implement a known interceptor interface for <{typeof(TArguments).Name}, {typeof(TResult).Name}>.")
                };

                completedCount++;

                if (beforeInvocationResult.IsT0)
                {
                    continue;
                }

                return (completedCount, beforeInvocationResult);
            }

            return (completedCount, new Success());
        }

        private OneOf<TResult, Exception> RunAfter(
            TArguments arguments,
            int completedCount,
            OneOf<TResult, Exception> resultOrError)
        {
            var afterInvocationResult = resultOrError;

            for (var i = completedCount - 1; i >= 0; i--)
            {
                try
                {
                    afterInvocationResult = _interceptors[i] switch
                    {
                        IProcedureInterceptor<TArguments, TResult> syncInterceptor =>
                            syncInterceptor.InvokeAfter(arguments, afterInvocationResult),

                        _ => throw new InvalidOperationException(
                            $"Interceptor {_interceptors[i].GetType()} does not implement a known interceptor interface for <{typeof(TArguments).Name}, {typeof(TResult).Name}>.")
                    };
                }
                catch (Exception exception)
                {
                    return exception;
                }
            }

            return afterInvocationResult;
        }
    }
}
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Lite.Procedures.Interceptors;
using OneOf;
using OneOf.Types;


namespace Lite.Procedures.Pipeline
{
    public sealed class AsyncProcedurePipeline<TArguments, TResult> : IAsyncProcedurePipeline<TArguments, TResult>
    {
        private readonly IAsyncProcedure<TArguments, TResult> _procedure;
        private readonly IProcedureInterceptorCore[] _interceptors;

        public AsyncProcedurePipeline(
            IAsyncProcedure<TArguments, TResult> procedure,
            IProcedureInterceptorCore[] interceptors)
        {
            _procedure = procedure;
            _interceptors = interceptors;
        }


        public async ValueTask<OneOf<TResult, Exception>> InvokeAsync(
            TArguments arguments,
            CancellationToken cancellationToken)
        {
            var (completedCount, beforeChainInvocationResult) = await RunBeforeAsync(arguments, cancellationToken);

            if (!beforeChainInvocationResult.TryPickT0(out _, out var resultOrException))
            {
                return await RunAfterAsync(arguments, completedCount, resultOrException, cancellationToken);
            }

            resultOrException = await InvokeProcedureAsync(arguments, cancellationToken);

            return await RunAfterAsync(arguments, completedCount, resultOrException, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<OneOf<TResult, Exception>> InvokeProcedureAsync(TArguments arguments,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _procedure.InvokeAsync(arguments, cancellationToken);
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private async ValueTask<(int completedCount, OneOf<Success, TResult, Exception>)> RunBeforeAsync(
            TArguments arguments,
            CancellationToken cancellationToken)
        {
            var completedCount = 0;

            foreach (var interceptor in _interceptors)
            {
                var beforeInvocationResult = interceptor switch
                {
                    IAsyncProcedureInterceptor<TArguments, TResult> asyncInterceptor =>
                        await asyncInterceptor.InvokeBeforeExecutionAsync(arguments, cancellationToken),

                    IProcedureInterceptor<TArguments, TResult> syncInterceptor => syncInterceptor.InvokeBefore(
                        arguments),

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

        private async ValueTask<OneOf<TResult, Exception>> RunAfterAsync(
            TArguments arguments,
            int completedCount,
            OneOf<TResult, Exception> resultOrError,
            CancellationToken cancellationToken)
        {
            var afterInvocationResult = resultOrError;

            for (var i = completedCount - 1; i >= 0; i--)
            {
                try
                {
                    afterInvocationResult = _interceptors[i] switch
                    {
                        IAsyncProcedureInterceptor<TArguments, TResult> asyncInterceptor =>
                            await asyncInterceptor.InvokeAfterExecutionAsync(arguments, afterInvocationResult,
                                cancellationToken),

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
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline
{
    public sealed class AsyncProcedurePipeline<TArguments, TResult> : IAsyncProcedure<TArguments, TResult>
    {
        private readonly Func<TArguments, CancellationToken, ValueTask<TResult>> _entry;

        public AsyncProcedurePipeline(
            IAsyncProcedure<TArguments, TResult> procedure,
            AsyncInterceptor<TArguments, TResult>[] interceptors)
        {
            Func<TArguments, CancellationToken, ValueTask<TResult>> next = procedure.ExecuteAsync;
            for (var i = interceptors.Length - 1; i >= 0; i--)
                next = new AsyncInterceptorRunner<TArguments, TResult>(interceptors[i], next).Delegate;
            _entry = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResult> ExecuteAsync(TArguments arguments, CancellationToken cancellationToken)
            => _entry(arguments, cancellationToken);
    }

    internal sealed class AsyncInterceptorRunner<TArguments, TResult>
    {
        private readonly AsyncInterceptor<TArguments, TResult> _interceptor;
        private readonly Func<TArguments, CancellationToken, ValueTask<TResult>> _next;

        public AsyncInterceptorRunner(
            AsyncInterceptor<TArguments, TResult> interceptor,
            Func<TArguments, CancellationToken, ValueTask<TResult>> next)
        {
            _interceptor = interceptor;
            _next = next;
        }

        public Func<TArguments, CancellationToken, ValueTask<TResult>> Delegate => InvokeAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<TResult> InvokeAsync(TArguments arguments, CancellationToken cancellationToken)
            => _interceptor.InvokeAsync(arguments, _next, cancellationToken);
    }
}

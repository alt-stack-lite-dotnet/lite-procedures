using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Pipeline
{
    public sealed class AsyncProcedurePipeline<TArguments, TResult> : IAsyncProcedurePipeline<TArguments, TResult>
    {
        private readonly Func<TArguments, CancellationToken, ValueTask<TResult>> _entry;

        public AsyncProcedurePipeline(
            IAsyncProcedure<TArguments, TResult> procedure,
            IAsyncProcedureInterceptor<TArguments, TResult>[] interceptors)
        {
            Func<TArguments, CancellationToken, ValueTask<TResult>> next = procedure.InvokeAsync;

            for (var i = interceptors.Length - 1; i >= 0; i--)
            {
                var interceptor = interceptors[i];
                var capturedNext = next;
                next = (args, ct) => interceptor.InvokeAsync(args, capturedNext, ct);
            }

            _entry = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResult> InvokeAsync(TArguments arguments, CancellationToken cancellationToken)
            => _entry(arguments, cancellationToken);
    }
}

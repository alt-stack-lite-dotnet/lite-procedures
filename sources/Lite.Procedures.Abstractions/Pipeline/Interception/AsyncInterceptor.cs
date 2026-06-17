using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Pipeline.Interception
{
    public abstract class AsyncInterceptor<TArguments, TResult> : IInterceptorCore
    {
        public abstract ValueTask<TResult> InvokeAsync(
            TArguments arguments,
            Func<TArguments, CancellationToken, ValueTask<TResult>> next,
            CancellationToken cancellationToken);
    }
}

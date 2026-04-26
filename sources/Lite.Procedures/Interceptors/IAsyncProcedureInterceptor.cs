using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Interceptors
{
    public interface IAsyncProcedureInterceptor<TArguments, TResult> : IProcedureInterceptorCore
    {
        ValueTask<TResult> InvokeAsync(
            TArguments arguments,
            Func<TArguments, CancellationToken, ValueTask<TResult>> next,
            CancellationToken cancellationToken);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Interceptors
{
    public interface IAsyncProcedureInterceptor<TArguments, TResult> : IProcedureInterceptorCore
    {
        ValueTask<OneOf<Success, TResult, Exception>> InvokeBeforeExecutionAsync(
            TArguments arguments,
            CancellationToken cancellationToken);

        ValueTask<OneOf<TResult, Exception>> InvokeAfterExecutionAsync(
            TArguments arguments,
            OneOf<TResult, Exception> resultOrError,
            CancellationToken cancellationToken) => 
                new  ValueTask<OneOf<TResult, Exception>>(resultOrError);
    }
}
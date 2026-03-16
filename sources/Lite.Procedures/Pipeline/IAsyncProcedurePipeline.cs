using System;
using System.Threading;
using System.Threading.Tasks;
using OneOf;


namespace Lite.Procedures.Pipeline
{
    public interface IAsyncProcedurePipeline<TArguments, TResult> : IProcedurePipelineCore<TArguments, TResult>
    {
        ValueTask<OneOf<TResult, Exception>> InvokeAsync(TArguments arguments, CancellationToken cancellationToken);
    }
}
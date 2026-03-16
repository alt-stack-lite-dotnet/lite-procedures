using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Pipeline
{
    public class AsyncPipelineAdapter<TArguments, TResult> : IAsyncProcedure<TArguments, TResult>
    {
        private readonly IAsyncProcedurePipeline<TArguments, TResult> _pipeline;

        public AsyncPipelineAdapter(IAsyncProcedurePipeline<TArguments, TResult> pipeline) => 
            _pipeline = pipeline;

        
        public async ValueTask<TResult> InvokeAsync(TArguments arguments, CancellationToken cancellationToken)
        {
            var resultOrError = await _pipeline.InvokeAsync(arguments, cancellationToken);

            return resultOrError.TryPickT0(out var result, out Exception? exception)
                ? result
                : throw new ProcedureCallException(exception);
        }
    }
}
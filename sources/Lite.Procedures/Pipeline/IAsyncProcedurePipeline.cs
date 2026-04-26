using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Pipeline
{
    public interface IAsyncProcedurePipeline<TArguments, TResult> : IProcedurePipelineCore<TArguments, TResult>
    {
        ValueTask<TResult> InvokeAsync(TArguments arguments, CancellationToken cancellationToken);
    }
}

namespace Lite.Procedures.Pipeline
{
    public interface IProcedurePipeline<TArguments, TResult> : IProcedurePipelineCore<TArguments, TResult>
    {
        TResult Invoke(TArguments arguments);
    }
}

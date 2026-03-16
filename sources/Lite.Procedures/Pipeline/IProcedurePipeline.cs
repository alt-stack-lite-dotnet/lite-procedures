using System;
using OneOf;

namespace Lite.Procedures.Pipeline
{
    public interface IProcedurePipeline<TArguments, TResult> : IProcedurePipelineCore<TArguments, TResult>
    {
        OneOf<TResult, Exception> Invoke(TArguments arguments);
    }
}
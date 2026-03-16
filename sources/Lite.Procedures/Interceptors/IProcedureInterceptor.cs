using System;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Interceptors
{
    public interface IProcedureInterceptor<TArguments, TResult> : IProcedureInterceptorCore
    {
        OneOf<Success, TResult, Exception> InvokeBefore(TArguments arguments);

        OneOf<TResult, Exception> InvokeAfter(TArguments arguments, OneOf<TResult, Exception> resultOrException) =>
            resultOrException;
    }
}
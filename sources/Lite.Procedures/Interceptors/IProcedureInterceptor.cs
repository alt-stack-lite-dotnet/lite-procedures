using System;

namespace Lite.Procedures.Interceptors
{
    public interface IProcedureInterceptor<TArguments, TResult> : IProcedureInterceptorCore
    {
        TResult Invoke(TArguments arguments, Func<TArguments, TResult> next);
    }
}

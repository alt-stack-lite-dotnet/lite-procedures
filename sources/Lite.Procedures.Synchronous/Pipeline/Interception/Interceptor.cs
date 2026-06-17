using System;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline.Interceptors
{
    public abstract class Interceptor<TArguments, TResult> : IInterceptorCore
    {
        public abstract TResult Invoke(TArguments arguments, Func<TArguments, TResult> next);
    }
}

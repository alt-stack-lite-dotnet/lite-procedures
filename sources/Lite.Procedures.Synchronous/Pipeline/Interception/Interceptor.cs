using System;

namespace Lite.Procedures.Pipeline.Interception
{
    public abstract class Interceptor<TArguments, TResult> : IInterceptorCore
    {
        public abstract TResult Invoke(TArguments arguments, Func<TArguments, TResult> next);
    }
}

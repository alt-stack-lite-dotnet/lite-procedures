using System;
using System.Runtime.CompilerServices;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline
{
    public sealed class ProcedurePipeline<TArguments, TResult> : IProcedure<TArguments, TResult>
    {
        private readonly Func<TArguments, TResult> _entry;

        public ProcedurePipeline(
            IProcedure<TArguments, TResult> procedure,
            Interceptor<TArguments, TResult>[] interceptors)
        {
            Func<TArguments, TResult> next = procedure.Execute;
            for (var i = interceptors.Length - 1; i >= 0; i--)
                next = new SyncInterceptorRunner<TArguments, TResult>(interceptors[i], next).Delegate;
            _entry = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Execute(TArguments arguments) => _entry(arguments);
    }

    internal sealed class SyncInterceptorRunner<TArguments, TResult>
    {
        private readonly Interceptor<TArguments, TResult> _interceptor;
        private readonly Func<TArguments, TResult> _next;

        public SyncInterceptorRunner(Interceptor<TArguments, TResult> interceptor, Func<TArguments, TResult> next)
        {
            _interceptor = interceptor;
            _next = next;
        }

        public Func<TArguments, TResult> Delegate => Invoke;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TResult Invoke(TArguments arguments) => _interceptor.Invoke(arguments, _next);
    }
}

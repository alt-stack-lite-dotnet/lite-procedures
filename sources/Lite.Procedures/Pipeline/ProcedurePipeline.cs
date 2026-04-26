using System;
using System.Runtime.CompilerServices;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Pipeline
{
    public sealed class ProcedurePipeline<TArguments, TResult> : IProcedurePipeline<TArguments, TResult>
    {
        private readonly Func<TArguments, TResult> _entry;

        public ProcedurePipeline(
            IProcedure<TArguments, TResult> procedure,
            IProcedureInterceptor<TArguments, TResult>[] interceptors)
        {
            Func<TArguments, TResult> next = procedure.Invoke;

            for (var i = interceptors.Length - 1; i >= 0; i--)
            {
                var interceptor = interceptors[i];
                var capturedNext = next;
                next = args => interceptor.Invoke(args, capturedNext);
            }

            _entry = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Invoke(TArguments arguments) => _entry(arguments);
    }
}

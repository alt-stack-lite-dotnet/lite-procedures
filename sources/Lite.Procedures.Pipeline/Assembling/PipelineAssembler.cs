using System;
using System.Collections.Generic;
using System.Linq;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline.Assembling
{
    public readonly struct AsyncInterceptorDescriptor<TArguments, TResult>
    {
        public AsyncInterceptorDescriptor(int priority, Func<AsyncInterceptor<TArguments, TResult>> factory)
        {
            Priority = priority;
            Factory = factory;
        }

        public int Priority { get; }
        public Func<AsyncInterceptor<TArguments, TResult>> Factory { get; }
    }

    /// <summary>
    /// Compositor of an async procedure + async interceptors into a runnable pipeline. Called
    /// once per pipeline registration (NOT on the hot invocation path) — factories are plain
    /// <see cref="Func{T}"/> delegates so callers can use lambdas, method groups, or closures
    /// over <see cref="IServiceProvider"/> as they prefer. Sync procedures live in
    /// Lite.Procedures.Synchronous with their own assembler; the two cannot be mixed.
    /// </summary>
    public sealed class PipelineAssembler
    {
        public IAsyncProcedure<TArguments, TResult> Assemble<TArguments, TResult>(
            Func<IAsyncProcedure<TArguments, TResult>> procedureFactory,
            IEnumerable<AsyncInterceptorDescriptor<TArguments, TResult>> interceptors)
        {
            if (procedureFactory == null) throw new ArgumentNullException(nameof(procedureFactory));
            if (interceptors == null) throw new ArgumentNullException(nameof(interceptors));

            var procedure = procedureFactory();
            var ordered = interceptors
                .OrderBy(d => d.Priority)
                .Select(d => d.Factory())
                .ToArray();

            // No interceptors -> no pipeline. Hand back the bare procedure so the call site is a
            // single dispatch into the handler, with no extra delegate hop (matches MessagePipe,
            // which returns the raw handler when no filters are attached). The public contract is
            // only IAsyncProcedure<,>.ExecuteAsync, so the concrete instance type is irrelevant.
            if (ordered.Length == 0)
                return procedure;

            return new AsyncProcedurePipeline<TArguments, TResult>(procedure, ordered);
        }
    }
}

using System;
using System.Collections.Generic;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Builders
{
    internal sealed class LiteProceduresPipelineBuilder<TProcedure>
        : ILiteProceduresPipelineBuilder<TProcedure>
    {
        private readonly List<Type> _interceptors;

        public LiteProceduresPipelineBuilder(IReadOnlyList<Type> globalInterceptors)
        {
            _interceptors = new List<Type>(globalInterceptors);
        }

        public ILiteProceduresPipelineBuilder<TProcedure> DropAllInterceptors<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore
        {
            _interceptors.RemoveAll(t => t == typeof(TInterceptor));
            return this;
        }

        public ILiteProceduresPipelineBuilder<TProcedure> AppendInterceptor<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore
        {
            _interceptors.Add(typeof(TInterceptor));
            return this;
        }

        public ILiteProceduresPipelineBuilder<TProcedure> PrependInterceptor<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore
        {
            _interceptors.Insert(0, typeof(TInterceptor));
            return this;
        }

        public IReadOnlyList<Type> Build() => _interceptors;
    }
}

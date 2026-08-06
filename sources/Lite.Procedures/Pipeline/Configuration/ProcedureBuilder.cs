using System;
using System.Collections.Generic;
using Lite.Procedures.Interception;

namespace Lite.Procedures.Configuration
{
    /// <summary>Per-procedure configuration sink. Collects public name/namespace and per-procedure interceptors.</summary>
    internal sealed class ProcedureBuilder : IProcedureBuilder
    {
        private readonly List<InterceptorRegistration> _interceptors = new List<InterceptorRegistration>();

        public IReadOnlyList<InterceptorRegistration> Interceptors => _interceptors;

        public void UseInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IInterceptorCore
            => _interceptors.Add(new InterceptorRegistration(typeof(TInterceptor), priority));

        public void UseInterceptor(IInterceptorCore interceptor, int priority = 0)
        {
            if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
            _interceptors.Add(new InterceptorRegistration(interceptor.GetType(), priority, interceptor));
        }

        public void UseInterceptor(Type interceptorType, int priority = 0)
        {
            if (interceptorType == null) throw new ArgumentNullException(nameof(interceptorType));
            _interceptors.Add(new InterceptorRegistration(interceptorType, priority));
        }
    }
}

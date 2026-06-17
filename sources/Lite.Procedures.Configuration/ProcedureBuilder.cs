using System;
using System.Collections.Generic;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Configuration
{
    /// <summary>Per-procedure configuration sink. Collects public name/namespace and per-procedure interceptors.</summary>
    internal sealed class ProcedureBuilder : IProcedureBuilder
    {
        private readonly List<InterceptorRegistration> _interceptors = new List<InterceptorRegistration>();

        public string? PublicName { get; private set; }
        public string? PublicNamespace { get; private set; }
        public IReadOnlyList<InterceptorRegistration> Interceptors => _interceptors;

        public void WithPublicName(string name) => PublicName = name;

        public void WithPublicNamespace(string name) => PublicNamespace = name;

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

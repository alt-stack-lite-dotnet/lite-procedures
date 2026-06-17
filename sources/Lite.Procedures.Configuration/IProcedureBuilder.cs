using System;
using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Configuration
{
    public interface IProcedureBuilder
    {
        void WithPublicName(string name);
        void WithPublicNamespace(string name);
        void UseInterceptor<TInterceptor>(int priority = 0) where TInterceptor : IInterceptorCore;
        void UseInterceptor(IInterceptorCore interceptor, int priority = 0);
        void UseInterceptor(Type interceptorType, int priority = 0);
    }
}
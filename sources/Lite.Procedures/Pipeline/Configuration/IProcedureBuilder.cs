using System;
using Lite.Procedures.Interception;

namespace Lite.Procedures.Configuration
{
    public interface IProcedureBuilder
    {
        void UseInterceptor<TInterceptor>(int priority = 0) where TInterceptor : IInterceptorCore;
        void UseInterceptor(IInterceptorCore interceptor, int priority = 0);
        void UseInterceptor(Type interceptorType, int priority = 0);
    }
}
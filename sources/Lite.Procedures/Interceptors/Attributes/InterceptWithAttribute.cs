using System;

namespace Lite.Procedures.Interceptors.Attributes
{
    public class InterceptWithAttribute : Attribute
    {
        public InterceptWithAttribute(Type interceptorType) => InterceptorType = interceptorType;
        
        public Type InterceptorType { get; }
    }
}
using System;

namespace Lite.Procedures.Interceptors.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class InterceptWithAttribute : Attribute
    {
        public InterceptWithAttribute(Type interceptorType) => InterceptorType = interceptorType;

        public Type InterceptorType { get; }
    }
}
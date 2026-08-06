using System;

namespace Lite.Procedures.Interception
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class InterceptWithAttribute : Attribute
    {
        public InterceptWithAttribute(Type interceptorType)
        {
            if (!typeof(IInterceptorCore).IsAssignableFrom(interceptorType))
                throw new ArgumentException($"Type {interceptorType} is not an IInterceptorCore.");     
            
            InterceptorType = interceptorType;
        }

        public Type InterceptorType { get; }
    }
}
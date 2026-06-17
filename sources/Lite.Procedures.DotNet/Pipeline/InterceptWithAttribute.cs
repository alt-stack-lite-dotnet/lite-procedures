using Lite.Procedures.Pipeline.Interception;

namespace Lite.Procedures.Pipeline;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class InterceptWithAttribute<TInterceptor>()
    : Lite.Procedures.Pipeline.Interception.InterceptWithAttribute(typeof(TInterceptor))
    where TInterceptor : IInterceptorCore;
using Lite.Procedures.Interception;

namespace Lite.Procedures;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class InterceptWithAttribute<TInterceptor>()
    : Lite.Procedures.Interception.InterceptWithAttribute(typeof(TInterceptor))
    where TInterceptor : IInterceptorCore;
namespace Lite.Procedures.Interceptors.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InterceptWithAttribute<TInterceptor> : InterceptWithAttribute
    where TInterceptor : IProcedureInterceptorCore
{
    public InterceptWithAttribute() : base(typeof(TInterceptor)) { }
}
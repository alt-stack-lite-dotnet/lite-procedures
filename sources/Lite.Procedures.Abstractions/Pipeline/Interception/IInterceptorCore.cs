namespace Lite.Procedures.Pipeline.Interception
{
    /// <summary>
    /// Non-generic marker for any interceptor — implemented by both
    /// <see cref="AsyncInterceptor{TArguments,TResult}"/>
    /// (this assembly) and <c>Interceptor&lt;,&gt;</c> (Lite.Procedures.Synchronous). There is
    /// deliberately NO generic <c>IInterceptorCore&lt;,&gt;</c>: sync and async interceptors share
    /// no typed supertype, so a sync interceptor cannot reach an async pipeline (or vice versa).
    /// </summary>
    public interface IInterceptorCore { }
}

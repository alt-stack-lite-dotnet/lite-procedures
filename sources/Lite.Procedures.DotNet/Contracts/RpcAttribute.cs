namespace Lite.Procedures.Contracts;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface, AllowMultiple =  true, Inherited = false)]
public class RpcAttribute<TResult> : RpcAttribute
{
    
}
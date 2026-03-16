namespace Lite.Procedures
{
    public interface IProcedure<in TArguments, out TResult> : IProcedureCore
    {
        TResult Invoke(TArguments arguments);
    }
}
namespace Lite.Procedures
{
    /// <summary>
    /// Non-generic marker for any procedure — implemented by both
    /// <see cref="IAsyncProcedure{TArguments,TResult}"/> and <c>IProcedure&lt;,&gt;</c> (sync,
    /// this assembly too). Used as a generic constraint
    /// where the concrete (TArguments, TResult) is not statically known (e.g. the builder API).
    /// There is deliberately NO generic <c>IProcedureCore&lt;,&gt;</c>: sync and async procedures
    /// share no typed supertype, so the two worlds cannot be mixed at compile time.
    /// </summary>
    public interface IProcedureCore { }
}

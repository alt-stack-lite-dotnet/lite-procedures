using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures
{
    public interface IAsyncProcedure<in TArguments, TResult> : IProcedureCore
    {
        ValueTask<TResult> ExecuteAsync(
            TArguments arguments,
            CancellationToken cancellationToken = default);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Streaming
{
    public struct AsyncReader<T>
    {
        public async Task<IAsyncEnumerable<T>> ReadAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();   
        }
    }
}
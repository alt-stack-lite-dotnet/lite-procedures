using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Streaming
{
    public struct Reader<T>
    {
        public async Task<IAsyncEnumerable<T>> ReadAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();   
        }
    }
}
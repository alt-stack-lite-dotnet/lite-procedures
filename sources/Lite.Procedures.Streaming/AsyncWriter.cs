using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Streaming
{
    public struct AsyncWriter<T>
    {
        public async ValueTask SendAsync(T value, CancellationToken cancellationToken)
        {
            
        }
    }
}
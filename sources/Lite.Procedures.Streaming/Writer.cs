using System.Threading;
using System.Threading.Tasks;

namespace Lite.Procedures.Streaming
{
    public struct Writer<T>
    {
        public async ValueTask SendAsync(T value, CancellationToken cancellationToken)
        {
            
        }
    }
}
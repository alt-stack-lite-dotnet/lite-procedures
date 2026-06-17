using System.IO;

namespace Lite.Procedures.Streaming
{
    public readonly struct Duplex
    {
        public Duplex(StreamReader reader, StreamWriter writer) => (Reader, Writer) = (reader, writer);

        public StreamReader Reader { get; }
        public StreamWriter Writer { get; }
    }
}
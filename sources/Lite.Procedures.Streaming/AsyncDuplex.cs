namespace Lite.Procedures.Streaming
{
    public readonly struct AsyncDuplex<TIn, TOut>
    {
        public AsyncDuplex(AsyncReader<TIn> reader, AsyncWriter<TOut> writer) => (Reader, Writer) = (reader, writer);

        public AsyncReader<TIn> Reader { get; }
        public AsyncWriter<TOut> Writer { get; }
    }
}
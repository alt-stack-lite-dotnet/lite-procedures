using System.Collections.Generic;

namespace Lite.Procedures.Rpc
{
    /// <summary>
    /// A source-generated carrier of <see cref="RpcDescriptor"/>s for one assembly. The generator
    /// emits an internal implementation and registers it into <see cref="RpcRegistry"/> via a
    /// <c>[ModuleInitializer]</c> at assembly load, mirroring how pipeline factories are registered.
    /// </summary>
    public interface IRpcProvider
    {
        /// <summary>The RPC descriptors discovered in the provider's assembly.</summary>
        IReadOnlyList<RpcDescriptor> Descriptors { get; }
    }
}

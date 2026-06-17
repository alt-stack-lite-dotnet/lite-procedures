using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lite.Procedures.Rpc
{
    /// <summary>
    /// Process-wide registry of RPC descriptors emitted by source generators. Generated providers
    /// register here via <c>[ModuleInitializer]</c> at assembly load; hosts read the descriptors
    /// (optionally filtered by scheme) to build gRPC routes and proto. Has no DI dependency, so
    /// generated code never references a DI package — mirrors <c>PipelineFactoryRegistry</c>.
    /// </summary>
    public static class RpcRegistry
    {
        private static readonly ConcurrentDictionary<IRpcProvider, byte> _providers
            = new ConcurrentDictionary<IRpcProvider, byte>();

        public static void Register(IRpcProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _providers.TryAdd(provider, 0);
        }

        /// <summary>All descriptors across every registered provider.</summary>
        public static IReadOnlyList<RpcDescriptor> Descriptors
            => _providers.Keys.SelectMany(p => p.Descriptors).ToArray();

        /// <summary>Descriptors that belong to the given scheme (ordinal match).</summary>
        public static IEnumerable<RpcDescriptor> ForScheme(string scheme)
        {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            return Descriptors.Where(d => d.Schemes.Contains(scheme, StringComparer.Ordinal));
        }

        /// <summary>Removes all registered providers. Intended for tests; do not call in production code.</summary>
        public static void Clear() => _providers.Clear();
    }
}

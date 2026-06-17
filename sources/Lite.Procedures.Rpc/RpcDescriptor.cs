using System;
using System.Collections.Generic;

namespace Lite.Procedures.Rpc
{
    /// <summary>
    /// Immutable RPC publication metadata for a single procedure. Produced by the source generator
    /// from <see cref="RpcAttribute"/> annotations and fluent descriptions, then registered into
    /// <see cref="RpcRegistry"/> via a module initializer. Downstream packages turn these descriptors
    /// into gRPC routes and proto definitions.
    /// </summary>
    public sealed class RpcDescriptor
    {
        public RpcDescriptor(
            Type argumentType,
            Type resultType,
            string service,
            string method,
            string? package = null,
            string? version = null,
            IReadOnlyList<string>? schemes = null,
            Type? procedureType = null)
        {
            ArgumentType = argumentType ?? throw new ArgumentNullException(nameof(argumentType));
            ResultType = resultType ?? throw new ArgumentNullException(nameof(resultType));
            Service = service ?? throw new ArgumentNullException(nameof(service));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Package = package;
            Version = version;
            Schemes = schemes ?? Array.Empty<string>();
            ProcedureType = procedureType;
        }

        /// <summary>The request/argument message type (proto request message).</summary>
        public Type ArgumentType { get; }

        /// <summary>The response/result message type (proto response message).</summary>
        public Type ResultType { get; }

        /// <summary>Logical service name, e.g. <c>"UserService"</c>.</summary>
        public string Service { get; }

        /// <summary>RPC method name, e.g. <c>"GetById"</c>.</summary>
        public string Method { get; }

        /// <summary>Package/namespace (proto package), e.g. <c>"org.myservice.Membership"</c>. May be null.</summary>
        public string? Package { get; }

        /// <summary>Service/contract version, e.g. <c>"1.0"</c>. May be null.</summary>
        public string? Version { get; }

        /// <summary>Schemes this procedure belongs to; a host exposes a chosen scheme. Never null (may be empty).</summary>
        public IReadOnlyList<string> Schemes { get; }

        /// <summary>The procedure implementation type when known (attribute path); null for fluent-only descriptions.</summary>
        public Type? ProcedureType { get; }
    }
}

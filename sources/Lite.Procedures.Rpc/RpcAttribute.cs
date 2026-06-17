using System;

namespace Lite.Procedures.Rpc
{
    /// <summary>
    /// Marks a procedure for RPC publication. The source generator reads these attributes (and the
    /// equivalent fluent descriptions) at compile time to emit the <c>RpcProvider</c> that feeds
    /// gRPC route/proto generation. Put this on a procedure implementing
    /// <c>IAsyncProcedure&lt;TArgument, TResult&gt;</c>; the argument/result types are taken from that interface.
    /// </summary>
    /// <example>
    /// <code>
    /// [Rpc(Method = "GetById", Service = "UserService", Package = "org.myservice.Membership", Version = "1.0", Schemes = "membership")]
    /// public sealed class GetUserByIdProcedure : IAsyncProcedure&lt;GetUserById, User&gt; { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RpcAttribute : Attribute
    {
        /// <summary>RPC method name, e.g. <c>"GetById"</c>.</summary>
        public string? Method { get; set; }

        /// <summary>Logical service name, e.g. <c>"UserService"</c>.</summary>
        public string? Service { get; set; }

        /// <summary>Package/namespace, e.g. <c>"org.myservice.Membership"</c> — becomes the proto package.</summary>
        public string? Package { get; set; }

        /// <summary>Service/contract version, e.g. <c>"1.0"</c>.</summary>
        public string? Version { get; set; }

        /// <summary>
        /// Comma-separated schemes this procedure belongs to, e.g. <c>"membership"</c> or <c>"membership,public"</c>.
        /// A host exposes a chosen scheme (e.g. <c>MapLiteGrpc(scheme: "membership")</c>).
        /// </summary>
        public string? Schemes { get; set; }
    }
}

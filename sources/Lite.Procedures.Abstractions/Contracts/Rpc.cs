using System;

namespace Lite.Procedures.Contracts
{
    public struct Rpc
    {
        private readonly (Type Type, RpcAttribute Metadata) _rpc;

        public Rpc((Type Type, RpcAttribute Metadata) rpc) => _rpc = rpc;

        public string MethodName => _rpc.Metadata.Method;
    }
}
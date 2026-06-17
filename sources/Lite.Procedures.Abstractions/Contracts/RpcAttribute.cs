using System;

namespace Lite.Procedures.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RpcAttribute : Attribute
    {
        public string Method { get; set; }
    }
}
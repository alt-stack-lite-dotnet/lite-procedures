using System;

namespace Lite.Procedures.Contracts
{
    public interface IRpcAttribute
    {
        public string Method { get; set; }
        public string Service { get; set; }
        public string Package { get; set; }
        
        public Type ProcedureType { get; }
        public Type ArgumentType { get; }
        public Type ResultType { get; }
    }
}
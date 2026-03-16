using System;

namespace Lite.Procedures.Pipeline
{
    public class ProcedureCallException : Exception
    {
        public ProcedureCallException(Exception reason) : base($"Error occured executing procedure", reason) { }
    }
}
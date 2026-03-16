using System;

namespace Lite.Procedures.Generated
{
    /// <summary>
    /// Registry of code-generated pipeline types. Implemented by source generator when pipeline codegen is used.
    /// </summary>
    public interface IGeneratedPipelineRegistry
    {
        /// <summary>
        /// Returns the generated pipeline type for the given procedure and interceptor chain, or null if none was generated.
        /// </summary>
        Type? GetGeneratedPipelineType(Type procedureType, Type[] interceptorTypes);
    }
}

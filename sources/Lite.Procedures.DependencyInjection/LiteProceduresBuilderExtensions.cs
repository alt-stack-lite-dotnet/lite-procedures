using System;
using Lite.Procedures.Configuration;
using Lite.Procedures;

namespace Lite.Procedures.DependencyInjection
{
    public static class LiteProceduresBuilderExtensions
    {
        /// <summary>
        /// Registers every procedure for which a source-generated factory exists in
        /// <see cref="PipelineFactoryRegistry"/>. Use this to avoid listing each
        /// <c>AddProcedure&lt;T&gt;()</c> by hand when the source generator already covers them.
        /// </summary>
        /// <remarks>
        /// The registry is populated by <c>[ModuleInitializer]</c>s emitted by
        /// <c>Lite.Procedures.Pipeline.SourceGenerators</c>. Vendor libraries that ship their own
        /// generator contribute to the same registry transparently.
        /// </remarks>
        public static ILiteProceduresBuilder AddAllProcedures(this ILiteProceduresBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            foreach (var procedureType in PipelineFactoryRegistry.ProcedureTypes)
                builder.AddProcedure(procedureType);

            return builder;
        }
    }
}

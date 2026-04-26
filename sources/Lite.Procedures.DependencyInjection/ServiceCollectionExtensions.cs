using System;
using Lite.Procedures.Builders;
using Lite.Procedures.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lite.Procedures.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLiteProcedures(
            this IServiceCollection services,
            Action<ILiteProceduresBuilder> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new LiteProceduresBuilder();
            configure(builder);
            var configuration = builder.Build();

            foreach (var entry in configuration.DefaultInterceptors)
                services.TryAddSingleton(entry.Type);

            foreach (var preset in configuration.Presets.Values)
                foreach (var entry in preset.Entries)
                    services.TryAddSingleton(entry.Type);

            var assembler = new PipelineAssembler();
            foreach (var procedure in configuration.Procedures)
            {
                services.TryAddSingleton(procedure.ProcedureType);

                foreach (var interceptorType in procedure.InterceptorTypes)
                    services.TryAddSingleton(interceptorType);

                var procedureType = procedure.ProcedureType;
                var interceptorTypes = procedure.InterceptorTypes;
                var pipelineInterface = PipelineAssembler.ResolvePipelineInterface(procedureType);

                services.AddSingleton(pipelineInterface,
                    sp => assembler.Assemble(procedureType, interceptorTypes, sp));
            }

            return services;
        }
    }
}

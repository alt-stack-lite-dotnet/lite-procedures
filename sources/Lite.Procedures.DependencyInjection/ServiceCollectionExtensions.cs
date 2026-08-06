using System;
using System.Linq;
using Lite.Procedures.Configuration;
using Lite.Procedures;
using Lite.Procedures.Assembling;
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

            var assembler = new PipelineAssembler();

            foreach (var procedure in configuration.Procedures)
            {
                services.TryAddSingleton(procedure.ProcedureType);
                foreach (var ir in procedure.Interceptors)
                {
                    if (ir.Instance is { } instance)
                        services.TryAddSingleton(ir.InterceptorType, _ => instance);
                    else
                        services.TryAddSingleton(ir.InterceptorType);
                }

                if (PipelineFactoryRegistry.TryGet(procedure.ProcedureType, out var factory))
                {
                    services.AddSingleton(
                        factory.ProcedureInterfaceType,
                        sp => factory.Assemble(sp, procedure.Interceptors
                            .Select(ir => (ir.InterceptorType, ir.Priority))
                            .ToArray()));
                    continue;
                }

                if (configuration.RequireGeneratedFactories)
                {
                    throw new InvalidOperationException(
                        $"Procedure '{procedure.ProcedureType.FullName}' has no source-generated factory, " +
                        $"but RequireGeneratedFactories() is enabled. Ensure the source generator covers this " +
                        $"procedure (mark its assembly with the generator) or remove the AoT-strict requirement.");
                }

                var procedureInterfaceType = PipelineAssemblerBridge.ResolveProcedureInterface(procedure.ProcedureType);
                var procedureType = procedure.ProcedureType;
                var interceptorTuples = procedure.Interceptors
                    .Select(ir => (ir.InterceptorType, ir.Priority))
                    .ToArray();

                services.AddSingleton(procedureInterfaceType, sp =>
                    PipelineAssemblerBridge.Assemble(
                        assembler,
                        procedureType,
                        interceptorTuples,
                        sp.GetRequiredService));
            }

            return services;
        }
    }
}

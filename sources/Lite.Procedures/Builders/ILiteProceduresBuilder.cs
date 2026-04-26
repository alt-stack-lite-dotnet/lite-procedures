using System;
using System.Reflection;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Builders
{
    public interface ILiteProceduresBuilder
    {
        ILiteProceduresBuilder AddProcedures(params Assembly[] assemblies);

        ILiteProceduresBuilder AddProcedure<TProcedure>() where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddProcedure<TProcedure>(string preset) where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddProcedureWithCustomPipeline<TProcedure>(
            Action<ILiteProceduresPipelineBuilder<TProcedure>> configure) where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddDefaultInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IProcedureInterceptorCore;

        ILiteProceduresBuilder AddDefaultInterceptors(params Assembly[] assemblies);

        ILiteProceduresBuilder AddPreset(string name, Action<IPipelinePresetBuilder> configure);
    }
}

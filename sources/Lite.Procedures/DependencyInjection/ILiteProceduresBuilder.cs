using System;
using System.Reflection;
using Lite.Procedures.Interceptors;

namespace Lite.Procedures.DependencyInjection
{
    public interface ILiteProceduresBuilder
    {
        ILiteProceduresBuilder AddProcedures(params Assembly[] assemblies);
        
        ILiteProceduresBuilder AddProcedure<TProcedure>() where TProcedure : IProcedureCore;

        ILiteProceduresBuilder AddProcedureWithCustomPipeline<TProcedure>(
            Action<ILiteProceduresPipelineBuilder<TProcedure>> configure) where TProcedure : IProcedureCore;
     
        ILiteProceduresBuilder AddGlobalInterceptor<TInterceptor>() where TInterceptor : IProcedureInterceptorCore;
        
        ILiteProceduresBuilder AddGlobalInterceptors(params Assembly[] assemblies);
    }
}
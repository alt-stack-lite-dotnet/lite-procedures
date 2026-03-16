using Lite.Procedures.Interceptors;

namespace Lite.Procedures.DependencyInjection
{
    public interface ILiteProceduresPipelineBuilder<TProcedure>
    {
        ILiteProceduresPipelineBuilder<TProcedure> DropAllInterceptors<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore; 
        
        ILiteProceduresPipelineBuilder<TProcedure> AppendInterceptor<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore; 
        
        ILiteProceduresPipelineBuilder<TProcedure> PrependInterceptor<TInterceptor>()
            where TInterceptor : IProcedureInterceptorCore; 
    }
}
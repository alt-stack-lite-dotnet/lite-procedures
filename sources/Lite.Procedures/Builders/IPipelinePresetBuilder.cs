using Lite.Procedures.Interceptors;

namespace Lite.Procedures.Builders
{
    public interface IPipelinePresetBuilder
    {
        IPipelinePresetBuilder AddInterceptor<TInterceptor>(int priority = 0)
            where TInterceptor : IProcedureInterceptorCore;

        IPipelinePresetBuilder InheritGlobals();

        IPipelinePresetBuilder IgnoreGlobals();
    }
}

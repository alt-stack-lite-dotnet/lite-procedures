using System;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLiteProcedures(
            this IServiceCollection @this,
            Action<ILiteProceduresBuilder> configure)
        {
            var builder = new LiteProceduresBuilder(@this);
            configure(@builder);
            return @this;
        }
    }
}
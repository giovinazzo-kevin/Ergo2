using Microsoft.Extensions.DependencyInjection;

namespace Ergo.Pipelines.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline<TPipeline>(this IServiceCollection services, Func<IServiceProvider, Pipeline, TPipeline> build)
        where TPipeline : class, IPipeline
    {
        //var envType = typeof(TPipeline)
        //    .GetInterfaces()
        //    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipeline<,,>))
        //    .Select(i => i.GetGenericArguments()[2])
        //    .Single();
        //var steps = envType.GetInterfaces()
        //    .SelectMany(envInterface => envInterface.Assembly.GetTypes()
        //        .Where(type => type.GetInterfaces()
        //            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipeline<,,>) && i.GetGenericArguments()[2].Equals(envInterface))
        //            .Any());
        return services.AddSingleton(sp => build(sp, new()));
    }
}


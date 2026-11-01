namespace HomeManager.Integration.Demo;

using Microsoft.Extensions.DependencyInjection;

public static class DemoServiceCollectionExtensions
{
    public static IServiceCollection AddDemoIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IIntegration, DemoIntegration>();
        return services;
    }
}

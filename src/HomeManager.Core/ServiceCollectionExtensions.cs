namespace HomeManager;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    [ServiceRegistration(Lifetime.Singleton, "Service$")]
    public static partial IServiceCollection AddCoreServices(this IServiceCollection services);

    // コアの3本柱 (イベントバス・ステートマシン・サービス) とエンティティ・インテグレーションの管理
    public static IServiceCollection AddCoreRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IStateMachine, StateMachine>();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        services.AddSingleton<EntityManager>();
        services.AddSingleton<IntegrationManager>();
        return services;
    }
}

namespace HomeManager;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    [ServiceRegistration(Lifetime.Singleton, "Service$")]
    public static partial IServiceCollection AddCoreServices(this IServiceCollection services);

    // DBを使うサービスをインターフェースでも引けるようにする (実装はAddCoreServicesが名前で登録する)
    public static IServiceCollection AddCoreServiceInterfaces(this IServiceCollection services)
    {
        services.AddSingleton<IAccountService>(static p => p.GetRequiredService<AccountService>());
        services.AddSingleton<ICoreConfigService>(static p => p.GetRequiredService<CoreConfigService>());
        services.AddSingleton<IOnboardingService>(static p => p.GetRequiredService<OnboardingService>());
        services.AddSingleton<ISystemInfoService>(static p => p.GetRequiredService<SystemInfoService>());
        return services;
    }

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

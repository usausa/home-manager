namespace HomeManager.Services;

// 全体設定のサービス (HA の homeassistant.reload_core_config)
public static class CoreConfigServices
{
    public const string ReloadCoreConfig = "reload_core_config";

    public static void Register(IServiceRegistry registry, ICoreConfigService coreConfig) =>
        registry.Register(
            new ServiceDefinition(Domains.HomeManager, ReloadCoreConfig)
            {
                Description = "全体設定を DB から読み直す"
            },
            (call, cancel) => coreConfig.LoadAsync(call.Context, cancel));
}

namespace HomeManager.Platforms;

internal static class SwitchServices
{
    public static void Register(IServiceRegistry registry, EntityManager entities)
    {
        registry.Register(
            new ServiceDefinition(Domains.Switch, PlatformServices.TurnOn)
            {
                Description = "スイッチを入れる",
                HasTarget = true,
                TargetDomain = Domains.Switch
            },
            (call, cancel) => entities.CallEntitiesAsync<SwitchEntity>(call, static (entity, c) => entity.TurnOnAsync(c), cancel));

        registry.Register(
            new ServiceDefinition(Domains.Switch, PlatformServices.TurnOff)
            {
                Description = "スイッチを切る",
                HasTarget = true,
                TargetDomain = Domains.Switch
            },
            (call, cancel) => entities.CallEntitiesAsync<SwitchEntity>(call, static (entity, c) => entity.TurnOffAsync(c), cancel));

        registry.Register(
            new ServiceDefinition(Domains.Switch, PlatformServices.Toggle)
            {
                Description = "スイッチを切り替える",
                HasTarget = true,
                TargetDomain = Domains.Switch
            },
            (call, cancel) => entities.CallEntitiesAsync<SwitchEntity>(call, static (entity, c) => entity.ToggleAsync(c), cancel));
    }
}

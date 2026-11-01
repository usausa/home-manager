namespace HomeManager.Platforms;

internal static class ButtonServices
{
    public static void Register(IServiceRegistry registry, EntityManager entities)
    {
        registry.Register(
            new ServiceDefinition(Domains.Button, PlatformServices.Press)
            {
                Description = "ボタンを押す",
                HasTarget = true,
                TargetDomain = Domains.Button
            },
            (call, cancel) => entities.CallEntitiesAsync<ButtonEntity>(call, (entity, c) => entity.HandlePressAsync(entities.UtcNow, c), cancel));
    }
}

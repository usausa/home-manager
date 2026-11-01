namespace HomeManager.Platforms;

internal static class LightServices
{
    private const string Brightness = "brightness";

    private const string BrightnessPercent = "brightness_pct";

    private const string Transition = "transition";

    private static readonly ServiceField[] TurnOnFields =
    [
        new(Brightness, "明るさ (0〜255)") { Example = "128" },
        new(BrightnessPercent, "明るさ (%)") { Example = "50" },
        new(Transition, "変化にかける秒数 (仮想デバイスでは使わない)") { Example = "2" }
    ];

    public static void Register(IServiceRegistry registry, EntityManager entities)
    {
        registry.Register(
            new ServiceDefinition(Domains.Light, PlatformServices.TurnOn)
            {
                Description = "照明をつける",
                HasTarget = true,
                TargetDomain = Domains.Light,
                Fields = TurnOnFields
            },
            (call, cancel) =>
            {
                var args = ParseTurnOnArgs(call);
                return entities.CallEntitiesAsync<LightEntity>(call, (entity, c) => TurnOnAsync(entity, args, c), cancel);
            });

        registry.Register(
            new ServiceDefinition(Domains.Light, PlatformServices.TurnOff)
            {
                Description = "照明を消す",
                HasTarget = true,
                TargetDomain = Domains.Light,
                Fields = [new(Transition, "変化にかける秒数 (仮想デバイスでは使わない)") { Example = "2" }]
            },
            (call, cancel) => entities.CallEntitiesAsync<LightEntity>(call, static (entity, c) => entity.TurnOffAsync(c), cancel));

        registry.Register(
            new ServiceDefinition(Domains.Light, PlatformServices.Toggle)
            {
                Description = "照明を切り替える (つけるときは明るさも指定できる)",
                HasTarget = true,
                TargetDomain = Domains.Light,
                Fields = TurnOnFields
            },
            (call, cancel) =>
            {
                var args = ParseTurnOnArgs(call);
                return entities.CallEntitiesAsync<LightEntity>(call, (entity, c) => entity.IsOn == true ? entity.TurnOffAsync(c) : TurnOnAsync(entity, args, c), cancel);
            });
    }

    // 明るさ0は消す (HAと同じ)
    private static ValueTask TurnOnAsync(LightEntity entity, LightTurnOnArgs args, CancellationToken cancel) =>
        args.Brightness == 0 ? entity.TurnOffAsync(cancel) : entity.TurnOnAsync(args, cancel);

    private static LightTurnOnArgs ParseTurnOnArgs(ServiceCall call)
    {
        var brightness = call.GetNumber(Brightness);
        if (brightness is < 0 or > 255)
        {
            throw new ServiceValidationException($"{Brightness} は 0〜255 で指定してください。");
        }

        var percent = call.GetNumber(BrightnessPercent);
        if (percent is < 0 or > 100)
        {
            throw new ServiceValidationException($"{BrightnessPercent} は 0〜100 で指定してください。");
        }

        if (percent is { } value)
        {
            brightness = (value * 255) / 100;
        }

        return new LightTurnOnArgs(brightness is { } result ? (int)Math.Round(result) : null, call.GetNumber(Transition));
    }
}

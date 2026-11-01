namespace HomeManager.Platforms;

// 種類を問わないON / OFF (HA の homeassistant.turn_on など)。対象をドメインごとに分け、各ドメインのサービスを呼ぶ
internal static class HomeManagerServices
{
    public static void Register(IServiceRegistry registry, ILogger log)
    {
        foreach (var (service, description) in new[]
        {
            (PlatformServices.TurnOn, "種類を問わずつける"),
            (PlatformServices.TurnOff, "種類を問わず消す"),
            (PlatformServices.Toggle, "種類を問わず切り替える")
        })
        {
            registry.Register(
                new ServiceDefinition(Domains.HomeManager, service)
                {
                    Description = description,
                    HasTarget = true
                },
                (call, cancel) => CallByDomainAsync(registry, log, call, cancel));
        }
    }

    private static async ValueTask CallByDomainAsync(IServiceRegistry registry, ILogger log, ServiceCall call, CancellationToken cancel)
    {
        foreach (var group in call.Target.EntityIds.GroupBy(EntityIds.GetDomain, StringComparer.Ordinal))
        {
            if ((group.Key == Domains.HomeManager) || !registry.Contains(group.Key, call.Service))
            {
                log.WarnServiceNotSupported(call.Domain, call.Service, group.Key);
                continue;
            }

            await registry.CallAsync(group.Key, call.Service, call.Data, new ServiceTarget([.. group]), call.Context, cancel);
        }
    }
}

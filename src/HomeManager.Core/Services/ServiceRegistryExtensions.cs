namespace HomeManager.Services;

public static class ServiceRegistryExtensions
{
    // 応答を返さないサービス
    public static void Register(this IServiceRegistry registry, ServiceDefinition definition, Func<ServiceCall, CancellationToken, ValueTask> handler) =>
        registry.Register(definition, async (call, cancel) =>
        {
            await handler(call, cancel);
            return null;
        });
}

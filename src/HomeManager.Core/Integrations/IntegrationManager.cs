namespace HomeManager.Integrations;

using System.Reflection;

// インテグレーションの読み込みと停止 (HA の bootstrap / loader)。起動と停止はHostのIntegrationWorkerが呼ぶ
public sealed class IntegrationManager
{
    private readonly Lock sync = new();

    private readonly List<IntegrationRuntime> runtimes = [];

    private readonly ILogger<IntegrationManager> log;

    private readonly IEnumerable<IIntegration> integrations;

    private readonly EntityManager entityManager;

    private readonly IServiceRegistry serviceRegistry;

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    public IntegrationManager(
        ILogger<IntegrationManager> log,
        IEnumerable<IIntegration> integrations,
        EntityManager entityManager,
        IServiceRegistry serviceRegistry,
        IEventBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.integrations = integrations;
        this.entityManager = entityManager;
        this.serviceRegistry = serviceRegistry;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    public IReadOnlyList<IntegrationInfo> ListIntegrations()
    {
        lock (sync)
        {
            return
            [
                .. runtimes.Select(x => new IntegrationInfo(
                    x.Domain,
                    x.Metadata.Name,
                    x.Metadata.IotClass,
                    x.Status,
                    entityManager.ListEntities(x.Domain).Count,
                    x.Error,
                    x.LoadedAt))
            ];
        }
    }

    public async ValueTask StartAsync(CancellationToken cancel)
    {
        // ドメインのサービス (light.turn_on など) を先に用意する
        PlatformServices.RegisterAll(serviceRegistry, entityManager, log);

        foreach (var integration in integrations)
        {
            var metadata = integration.GetType().GetCustomAttribute<IntegrationAttribute>();
            if (metadata is null)
            {
                log.WarnIntegrationMetadataMissing(integration.GetType().FullName ?? integration.GetType().Name);
                continue;
            }

            var runtime = new IntegrationRuntime(log, entityManager, integration, metadata);
            lock (sync)
            {
                runtimes.Add(runtime);
            }

            await SetupAsync(runtime, cancel);
        }
    }

    // 1つの失敗で他を止めない (HAと同じく失敗したものだけSetupErrorにする)
    private async ValueTask SetupAsync(IntegrationRuntime runtime, CancellationToken cancel)
    {
        var domain = runtime.Domain;
        try
        {
            await runtime.Integration.SetupAsync(runtime, cancel);

            runtime.Status = IntegrationStatus.Loaded;
            runtime.LoadedAt = timeProvider.GetUtcNow();
            log.InfoIntegrationLoaded(domain);
            bus.Publish(new ComponentLoadedEvent(domain, EventContext.Create(), timeProvider.GetUtcNow()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            runtime.Status = IntegrationStatus.SetupError;
            runtime.Error = ex.Message;
            log.ErrorIntegrationSetup(domain, ex);

            await runtime.StopBackgroundAsync();
            entityManager.RemovePlatform(domain);
        }
    }

    public async ValueTask StopAsync(CancellationToken cancel)
    {
        IntegrationRuntime[] targets;
        lock (sync)
        {
            targets = [.. runtimes];
            runtimes.Clear();
        }

        // 読み込んだ順の逆に止める
        for (var i = targets.Length - 1; i >= 0; i--)
        {
            var runtime = targets[i];
            await runtime.StopBackgroundAsync();
            if (runtime.Status == IntegrationStatus.Loaded)
            {
                try
                {
                    await runtime.Integration.UnloadAsync(cancel);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorIntegrationUnload(runtime.Domain, ex);
                }
            }

            entityManager.RemovePlatform(runtime.Domain);
            runtime.Dispose();
            log.InfoIntegrationUnloaded(runtime.Domain);
        }
    }
}

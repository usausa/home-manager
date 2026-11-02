namespace HomeManager.Host.Workers;

// インテグレーションの起動と停止 (HA の bootstrap)。停止はインテグレーションを読み込んだ順の逆に行う
public sealed class IntegrationWorker : BackgroundService
{
    private readonly ILogger<IntegrationWorker> log;

    private readonly IntegrationManager manager;

    private readonly IServiceRegistry registry;

    private readonly ICoreConfigService coreConfig;

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    public IntegrationWorker(
        ILogger<IntegrationWorker> log,
        IntegrationManager manager,
        IServiceRegistry registry,
        ICoreConfigService coreConfig,
        IEventBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.manager = manager;
        this.registry = registry;
        this.coreConfig = coreConfig;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.InfoWorkerStart(nameof(IntegrationWorker));
        try
        {
            bus.Publish(new SystemEvent(EventTypes.HomeManagerStart, EventContext.Create(), timeProvider.GetUtcNow()));
            // コアのサービス (homemanager.reload_core_config) はインテグレーションより先に用意する
            CoreConfigServices.Register(registry, coreConfig);
            await manager.StartAsync(stoppingToken);
            bus.Publish(new SystemEvent(EventTypes.HomeManagerStarted, EventContext.Create(), timeProvider.GetUtcNow()));

            await Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            bus.Publish(new SystemEvent(EventTypes.HomeManagerStop, EventContext.Create(), timeProvider.GetUtcNow()));
            await manager.StopAsync(CancellationToken.None);
            log.InfoWorkerStop(nameof(IntegrationWorker));
        }
    }
}

namespace HomeManager.Host.Workers;

// インテグレーションの起動と停止 (HA の bootstrap)。停止はインテグレーションを読み込んだ順の逆に行う
public sealed class IntegrationWorker : BackgroundService
{
    private readonly ILogger<IntegrationWorker> log;

    private readonly IntegrationManager manager;

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    public IntegrationWorker(
        ILogger<IntegrationWorker> log,
        IntegrationManager manager,
        IEventBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.manager = manager;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.InfoWorkerStart(nameof(IntegrationWorker));
        try
        {
            bus.Publish(new SystemEvent(EventTypes.HomeManagerStart, EventContext.Create(), timeProvider.GetUtcNow()));
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

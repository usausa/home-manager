namespace HomeManager;

using HomeManager.Platforms;
using HomeManager.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

// コアの3本柱とエンティティの管理を組み立てる (時刻はFakeTimeProvider)
public sealed class TestRuntime
{
    public static readonly DateTimeOffset Start = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    public TestRuntime()
    {
        Time = new FakeTimeProvider(Start);
        Bus = new EventBus(NullLogger<EventBus>.Instance);
        States = new StateMachine(Bus, Time);
        Services = new ServiceRegistry(Bus, Time);
        Entities = new EntityManager(NullLogger<EntityManager>.Instance, States, Time);
        PlatformServices.RegisterAll(Services, Entities, NullLogger.Instance);
    }

    public FakeTimeProvider Time { get; }

    public EventBus Bus { get; }

    public StateMachine States { get; }

    public ServiceRegistry Services { get; }

    public EntityManager Entities { get; }
}

namespace HomeManager.Integrations;

using HomeManager.Domain;
using HomeManager.Integration.Demo;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class IntegrationManagerTests
{
    [Fact]
    public async Task StartLoadsIntegrationsAndKeepsFailureSeparate()
    {
        // Arrange
        var runtime = new TestRuntime();
        var manager = CreateManager(runtime, new FailingIntegration(), new DemoIntegration(NullLogger<DemoIntegration>.Instance, runtime.Time));
        using var loaded = new EventRecorder<ComponentLoadedEvent>(runtime.Bus);

        // Act
        await manager.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        var integrations = manager.ListIntegrations();
        var failing = Assert.Single(integrations, static x => x.Domain == "failing");
        var demo = Assert.Single(integrations, static x => x.Domain == DemoIntegration.Domain);
        Assert.Equal(IntegrationStatus.SetupError, failing.Status);
        Assert.Equal("Setup failed.", failing.Error);
        Assert.Equal(IntegrationStatus.Loaded, demo.Status);
        Assert.Equal(14, demo.EntityCount);
        Assert.Equal(DemoIntegration.Domain, (await loaded.NextAsync()).Component);
        Assert.Equal(StateValues.On, runtime.States.GetState("light.living_ceiling")?.State);

        await manager.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DemoSensorsChangeOnInterval()
    {
        // Arrange
        var runtime = new TestRuntime();
        var manager = CreateManager(runtime, new DemoIntegration(NullLogger<DemoIntegration>.Instance, runtime.Time));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        using var written = new EventRecorder<IHomeEvent>(runtime.Bus, static x => x is StateChangedEvent { EntityId: "sensor.washer_power" } or StateReportedEvent { EntityId: "sensor.washer_power" });

        // Act
        // バックグラウンドの処理がタイマーを作るまで待てないため、届くまで時間を進める
        IHomeEvent? value = null;
        for (var i = 0; (i < 50) && (value is null); i++)
        {
            runtime.Time.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(20, TestContext.Current.CancellationToken);
            written.TryNext(out value);
        }

        // Assert
        Assert.NotNull(value);

        await manager.StopAsync(TestContext.Current.CancellationToken);
    }

    // 停止しても状態は残す (HA と同じ。消すと画面や履歴に全エンティティの削除が出る)
    [Fact]
    public async Task StopKeepsStates()
    {
        // Arrange
        var runtime = new TestRuntime();
        var manager = CreateManager(runtime, new DemoIntegration(NullLogger<DemoIntegration>.Instance, runtime.Time));
        await manager.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await manager.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(14, runtime.States.Count);
        Assert.Empty(manager.ListIntegrations());
    }

    // 止めるよう求めていないキャンセル (インテグレーション自身のタイムアウトなど) はセットアップの失敗として扱い、ほかは読み込む
    [Fact]
    public async Task CanceledSetupIsSetupError()
    {
        // Arrange
        var runtime = new TestRuntime();
        var manager = CreateManager(runtime, new TimeoutIntegration(), new DemoIntegration(NullLogger<DemoIntegration>.Instance, runtime.Time));

        // Act
        await manager.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        var integrations = manager.ListIntegrations();
        Assert.Equal(IntegrationStatus.SetupError, Assert.Single(integrations, static x => x.Domain == "timeout").Status);
        Assert.Equal(IntegrationStatus.Loaded, Assert.Single(integrations, static x => x.Domain == DemoIntegration.Domain).Status);

        await manager.StopAsync(TestContext.Current.CancellationToken);
    }

    private static IntegrationManager CreateManager(TestRuntime runtime, params IIntegration[] integrations) =>
        new(NullLogger<IntegrationManager>.Instance, integrations, runtime.Entities, runtime.Services, runtime.Bus, runtime.Time);

    [Integration("timeout", Name = "Timeout")]
    private sealed class TimeoutIntegration : IIntegration
    {
        public ValueTask SetupAsync(IIntegrationContext context, CancellationToken cancel) =>
            throw new OperationCanceledException("Connection timed out.");
    }

    [Integration("failing", Name = "Failing")]
    private sealed class FailingIntegration : IIntegration
    {
        public ValueTask SetupAsync(IIntegrationContext context, CancellationToken cancel) =>
            throw new InvalidOperationException("Setup failed.");
    }
}

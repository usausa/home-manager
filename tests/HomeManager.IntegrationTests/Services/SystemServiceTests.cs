namespace HomeManager.Services;

using System.Text.Json;

using HomeManager.Accessors;
using HomeManager.Domain;
using HomeManager.Integrations;

using Microsoft.Extensions.DependencyInjection;

// 全体設定・初回セットアップ・システム情報 (Settingの表とシステムの情報)
public sealed class SystemServiceTests : IClassFixture<TestApplicationFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TestApplicationFactory factory;

    public SystemServiceTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CoreConfigUpdateSavesAndPublishesChanges()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<ICoreConfigService>();
        var bus = factory.Services.GetRequiredService<IEventBus>();
        var cancel = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<CoreConfigUpdatedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = bus.Subscribe<CoreConfigUpdatedEvent>((x, _) =>
        {
            received.TrySetResult(x);
            return ValueTask.CompletedTask;
        });
        var current = service.Config;

        // Act
        var status = await service.UpdateAsync(current with { Name = "別荘", TimeZone = "Europe/London" }, EventContext.Create("test"), cancel);
        var stale = await service.UpdateAsync(current with { Name = "古い値" }, EventContext.Create("test"), cancel);
        var value = await received.Task.WaitAsync(Timeout, cancel);

        // Assert
        Assert.True(current.Version > 0);
        Assert.Equal(DataWriteStatus.Success, status);
        Assert.Equal(DataWriteStatus.Conflict, stale);
        Assert.Equal("別荘", service.Config.Name);
        Assert.Equal(current.Version + 1, service.Config.Version);
        Assert.Equal("Europe/London", service.TimeZone.Id);
        Assert.Equal(2, value.Data.Count);
        Assert.Equal("別荘", value.Data["name"]);
        Assert.Equal("Europe/London", value.Data["time_zone"]);
        Assert.Equal("test", value.Context.UserId);
    }

    // ほかのプロセスがDBを書き換えても、homemanager.reload_core_configで読み直せる
    [Fact]
    public async Task ReloadCoreConfigPicksUpExternalChange()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<ICoreConfigService>();
        var accessor = factory.Services.GetRequiredService<SettingAccessor>();
        var registry = factory.Services.GetRequiredService<IServiceRegistry>();
        var cancel = TestContext.Current.CancellationToken;
        // サービスは起動の後にIntegrationWorkerが登録する
        await WaitUntilAsync(() => Task.FromResult(registry.Contains(Domains.HomeManager, CoreConfigServices.ReloadCoreConfig)), cancel);
        var current = service.Config;
        var json = JsonSerializer.Serialize(current with { Name = "海の家" }, JsonOptions);
        await accessor.UpdateAsync("core.config", json, TimeProvider.System.GetUtcNow().UtcDateTime, current.Version, cancel);

        // Act
        await registry.CallAsync(Domains.HomeManager, CoreConfigServices.ReloadCoreConfig, null, null, EventContext.Create("test"), cancel);

        // Assert
        Assert.Equal("海の家", service.Config.Name);
        Assert.Equal(current.Version + 1, service.Config.Version);
    }

    // 形が違う JSON は既定値で読む (起動や読み直しを止めない)
    [Fact]
    public async Task InvalidSettingJsonUsesDefaults()
    {
        // Arrange
        var coreConfig = factory.Services.GetRequiredService<ICoreConfigService>();
        var onboarding = factory.Services.GetRequiredService<IOnboardingService>();
        var accessor = factory.Services.GetRequiredService<SettingAccessor>();
        var cancel = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        await accessor.UpsertAsync("core.config", """{ "name": 1 }""", now, cancel);
        await accessor.UpsertAsync("core.onboarding", """{ "done": "core_config" }""", now, cancel);

        // Act
        await coreConfig.LoadAsync(EventContext.Create("test"), cancel);
        await onboarding.LoadAsync(cancel);

        // Assert
        Assert.Equal(new CoreConfig().Name, coreConfig.Config.Name);
        Assert.False(onboarding.IsDone(OnboardingSteps.CoreConfig));
    }

    [Fact]
    public async Task OnboardingStepIsSaved()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<IOnboardingService>();
        var cancel = TestContext.Current.CancellationToken;
        var before = service.IsDone(OnboardingSteps.CoreConfig);

        // Act
        await service.CompleteAsync(OnboardingSteps.CoreConfig, cancel);
        await service.LoadAsync(cancel);

        // Assert
        Assert.False(before);
        Assert.True(service.IsDone(OnboardingSteps.CoreConfig));
    }

    [Fact]
    public async Task SystemInfoIncludesDatabaseAndIntegrations()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<ISystemInfoService>();
        var cancel = TestContext.Current.CancellationToken;
        SystemInfo? info = null;

        // Act (インテグレーションの読み込みは起動の後に進むため、読み込まれるまで待つ)
        await WaitUntilAsync(
            async () =>
            {
                info = await service.GetInfoAsync(cancel);
                return info.Integrations.Any(static x => x.Status == IntegrationStatus.Loaded);
            },
            cancel);

        // Assert
        Assert.NotNull(info?.Database);
        Assert.StartsWith("3.", info.Database.Version, StringComparison.Ordinal);
        Assert.EndsWith(".db", info.Database.File, StringComparison.Ordinal);
        Assert.True(info.Database.Size > 0);
        Assert.StartsWith("0.0.1", info.Version, StringComparison.Ordinal);
        Assert.True(info.EntityCount > 0);
        Assert.Contains(info.Integrations, static x => x.Domain == "demo");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken cancel)
    {
        var limit = TimeProvider.System.GetUtcNow() + Timeout;
        while (!await condition())
        {
            if (TimeProvider.System.GetUtcNow() > limit)
            {
                throw new TimeoutException("Condition was not satisfied.");
            }

            await Task.Delay(100, cancel);
        }
    }
}

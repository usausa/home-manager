namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Host.Application.Circuits;
using HomeManager.Host.Components.Pages;
using HomeManager.Integrations;
using HomeManager.Services;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

public sealed class SystemInfoPageTests : SessionTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly ISystemInfoService systemInfoService = Substitute.For<ISystemInfoService>();

    private readonly CircuitTracker tracker = new();

    public SystemInfoPageTests()
    {
        Services.AddSingleton(systemInfoService);
        Services.AddSingleton(tracker);
        SignIn(CreateProfile());
        tracker.Add(new CircuitInfo("a", "admin", Now, true));
    }

    [Fact]
    public void RenderShowsVersionDatabaseIntegrationsAndCircuits()
    {
        // Arrange
        systemInfoService.GetInfoAsync(Arg.Any<CancellationToken>()).Returns(CreateInfo(new DatabaseInfo("18.0", 8 * 1024 * 1024)));

        // Act
        var cut = Render<SystemInfoPage>();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("0.0.1（4b56081）", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("PostgreSQL 18.0", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("8.0 MB", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("デモ (仮想デバイス)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("読み込み済み", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("2026/11/01 18:00:00", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("接続中の回線（1）", cut.Markup, StringComparison.Ordinal);
    }

    // ブラウザのタブが増えるとその場で回線の一覧が変わる
    [Fact]
    public void CircuitChangeIsReflected()
    {
        // Arrange
        systemInfoService.GetInfoAsync(Arg.Any<CancellationToken>()).Returns(CreateInfo(new DatabaseInfo("18.0", 1024)));
        var cut = Render<SystemInfoPage>();

        // Act
        tracker.Add(new CircuitInfo("b", "machi", Now.AddMinutes(1), false));

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("接続中の回線（2）", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("再接続待ち", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseErrorIsShown()
    {
        // Arrange
        systemInfoService.GetInfoAsync(Arg.Any<CancellationToken>()).Returns(CreateInfo(null));

        // Act
        var cut = Render<SystemInfoPage>();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("データベースに接続できません。", cut.Markup, StringComparison.Ordinal));
    }

    private static SystemInfo CreateInfo(DatabaseInfo? database) =>
        new(
            "0.0.1+4b560815f3a07bf21ea63eec5ad57d9b4547dc28",
            ".NET 10.0.0",
            "Windows",
            Now,
            database,
            14,
            20,
            [new IntegrationInfo("demo", "デモ (仮想デバイス)", IotClass.Calculated, IntegrationStatus.Loaded, 14, null, Now)]);
}

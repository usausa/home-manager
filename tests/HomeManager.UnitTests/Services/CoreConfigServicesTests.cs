namespace HomeManager.Services;

using HomeManager.Domain;

using NSubstitute;

public sealed class CoreConfigServicesTests
{
    [Fact]
    public async Task ReloadCoreConfigLoadsWithCallerContext()
    {
        // Arrange
        var runtime = new TestRuntime();
        var coreConfig = Substitute.For<ICoreConfigService>();
        CoreConfigServices.Register(runtime.Services, coreConfig);

        // Act
        await runtime.Services.CallAsync(Domains.HomeManager, CoreConfigServices.ReloadCoreConfig, null, null, EventContext.Create("admin"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(runtime.Services.Contains(Domains.HomeManager, CoreConfigServices.ReloadCoreConfig));
        await coreConfig.Received(1).LoadAsync(Arg.Is<EventContext>(static x => x.UserId == "admin"), Arg.Any<CancellationToken>());
    }
}

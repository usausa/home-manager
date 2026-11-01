namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Entities;
using HomeManager.Host.Components.Pages;
using HomeManager.Integration.Demo;
using HomeManager.Services;

using Microsoft.Extensions.DependencyInjection;

public sealed class OverviewPageTests : MudBlazorTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly TestRuntime runtime = new();

    public OverviewPageTests()
    {
        Services.AddSingleton<IEventBus>(runtime.Bus);
        Services.AddSingleton<IStateMachine>(runtime.States);
        Services.AddSingleton<IServiceRegistry>(runtime.Services);
        Services.AddScoped<EntityOperations>();
        AddAuthorization().SetAuthorized("admin");

        runtime.Entities.AddEntities("demo",
        [
            new DemoLight("ceiling", "天井照明", true, false, 255),
            new DemoSensor("room", "室温", DeviceClasses.Temperature, "°C", 23.5, null)
        ]);
    }

    [Fact]
    public void RenderGroupsTilesByDomain()
    {
        // Act
        var cut = Render<OverviewPage>();

        // Assert
        Assert.Equal(2, cut.FindAll(".entity-tile").Count);
        Assert.Contains("💡 照明", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("23.5 °C", cut.Markup, StringComparison.Ordinal);
    }

    // P1の完了の目安: 片方の画面で仮想ライトを切り替えると、もう片方に即座に反映される
    [Fact]
    public void ToggleInOneViewIsReflectedInAnotherView()
    {
        // Arrange
        var first = Render<OverviewPage>();
        var second = Render<OverviewPage>();

        // Act
        first.Find(".entity-tile button").Click();

        // Assert
        second.WaitForAssertion(() => Assert.Contains("オン・100%", second.Find(".entity-tile").TextContent, StringComparison.Ordinal), Timeout);
        Assert.Equal("admin", runtime.States.GetState("light.ceiling")?.Context.UserId);
    }

    [Fact]
    public void SensorUpdateIsReflected()
    {
        // Arrange
        var cut = Render<OverviewPage>();

        // Act
        runtime.States.SetState("sensor.room", "24.0", new Dictionary<string, object?> { [AttributeNames.UnitOfMeasurement] = "°C" }, EventContext.Create());

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("24.0 °C", cut.Markup, StringComparison.Ordinal), Timeout);
    }
}

namespace HomeManager.Platforms;

using System.Text.Json.Nodes;

using HomeManager.Domain;
using HomeManager.Integration.Demo;
using HomeManager.Services;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class PlatformServicesTests
{
    [Fact]
    public async Task LightTurnOnSetsBrightnessAndOperator()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoLight("ceiling", "天井照明", true, false, 255)]);

        // Act
        await CallAsync(runtime, Domains.Light, PlatformServices.TurnOn, "light.ceiling", new JsonObject { ["brightness_pct"] = 50 }, "admin");

        // Assert
        var state = runtime.States.GetState("light.ceiling");
        Assert.Equal(StateValues.On, state?.State);
        Assert.Equal(128, state?.GetAttribute(AttributeNames.Brightness));
        Assert.Equal("admin", state?.Context.UserId);
    }

    [Fact]
    public async Task LightBrightnessZeroTurnsOffAndToggleTurnsOn()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoLight("ceiling", "天井照明", true, true, 100)]);

        // Act
        await CallAsync(runtime, Domains.Light, PlatformServices.TurnOn, "light.ceiling", new JsonObject { ["brightness"] = 0 });
        var off = runtime.States.GetState("light.ceiling");
        await CallAsync(runtime, Domains.Light, PlatformServices.Toggle, "light.ceiling");

        // Assert
        Assert.Equal(StateValues.Off, off?.State);
        Assert.Null(off?.GetAttribute(AttributeNames.Brightness));
        Assert.Equal(StateValues.On, runtime.States.GetState("light.ceiling")?.State);
        Assert.Equal(100, runtime.States.GetState("light.ceiling")?.GetAttribute(AttributeNames.Brightness));
    }

    [Fact]
    public async Task LightRejectsInvalidBrightness()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoLight("ceiling", "天井照明", true, false, 255)]);

        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => await CallAsync(runtime, Domains.Light, PlatformServices.TurnOn, "light.ceiling", new JsonObject { ["brightness_pct"] = 120 }));
    }

    [Fact]
    public async Task HomeManagerTurnOffCallsEachDomain()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoLight("ceiling", "天井照明", true, true, 255), new DemoSwitch("tv", "テレビ", null, true)]);

        // Act
        await runtime.Services.CallAsync(Domains.HomeManager, PlatformServices.TurnOff, null, ServiceTarget.Of("light.ceiling", "switch.tv", "sensor.unknown"), EventContext.Create(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(StateValues.Off, runtime.States.GetState("light.ceiling")?.State);
        Assert.Equal(StateValues.Off, runtime.States.GetState("switch.tv")?.State);
    }

    [Fact]
    public async Task NumberSetValueChecksRange()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoNumber("volume", "音量", 30, 0, 100, 1, "%")]);

        // Act
        await CallAsync(runtime, Domains.Number, PlatformServices.SetValue, "number.volume", new JsonObject { ["value"] = 45 });

        // Assert
        Assert.Equal("45", runtime.States.GetState("number.volume")?.State);
        Assert.Equal(100d, runtime.States.GetState("number.volume")?.GetAttribute(AttributeNames.Max));
        await Assert.ThrowsAsync<ServiceValidationException>(async () => await CallAsync(runtime, Domains.Number, PlatformServices.SetValue, "number.volume", new JsonObject { ["value"] = 101 }));
    }

    [Fact]
    public async Task SelectServicesMoveThroughOptions()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoSelect("speed", "風量", ["弱", "中", "強"], "中")]);

        // Act
        await CallAsync(runtime, Domains.Select, PlatformServices.SelectNext, "select.speed");
        var next = runtime.States.GetState("select.speed")?.State;
        await CallAsync(runtime, Domains.Select, PlatformServices.SelectNext, "select.speed");
        var cycled = runtime.States.GetState("select.speed")?.State;
        await CallAsync(runtime, Domains.Select, PlatformServices.SelectPrevious, "select.speed", new JsonObject { ["cycle"] = false });
        var notCycled = runtime.States.GetState("select.speed")?.State;
        await CallAsync(runtime, Domains.Select, PlatformServices.SelectOption, "select.speed", new JsonObject { ["option"] = "中" });

        // Assert
        Assert.Equal("強", next);
        Assert.Equal("弱", cycled);
        Assert.Equal("弱", notCycled);
        Assert.Equal("中", runtime.States.GetState("select.speed")?.State);
        await Assert.ThrowsAsync<ServiceValidationException>(async () => await CallAsync(runtime, Domains.Select, PlatformServices.SelectOption, "select.speed", new JsonObject { ["option"] = "最強" }));
    }

    [Fact]
    public async Task ButtonPressWritesPressedTime()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Entities.AddEntities("demo", [new DemoButton("chime", "チャイム", NullLogger.Instance)]);
        var initial = runtime.States.GetState("button.chime")?.State;
        runtime.Time.Advance(TimeSpan.FromMinutes(1));

        // Act
        await CallAsync(runtime, Domains.Button, PlatformServices.Press, "button.chime", null, "admin");

        // Assert
        var state = runtime.States.GetState("button.chime");
        Assert.Equal(StateValues.Unknown, initial);
        Assert.Equal(TestRuntime.Start.AddMinutes(1), DateTimeOffset.Parse(state!.State, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("admin", state.Context.UserId);
    }

    [Fact]
    public async Task ContextExpiresForLaterWrites()
    {
        // Arrange
        var runtime = new TestRuntime();
        var entity = new DemoSwitch("tv", "テレビ", null, false);
        runtime.Entities.AddEntities("demo", [entity]);
        await CallAsync(runtime, Domains.Switch, PlatformServices.TurnOn, "switch.tv", null, "admin");

        // Act
        runtime.Time.Advance(TimeSpan.FromSeconds(10));
        await entity.TurnOffAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(runtime.States.GetState("switch.tv")?.Context.UserId);
    }

    private static async Task CallAsync(TestRuntime runtime, string domain, string service, string entityId, JsonObject? data = null, string? user = null) =>
        await runtime.Services.CallAsync(domain, service, data, ServiceTarget.Of(entityId), EventContext.Create(user), TestContext.Current.CancellationToken);
}

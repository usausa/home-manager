namespace HomeManager.Platforms;

using HomeManager.Domain;
using HomeManager.Integration.Demo;

public sealed class EntityManagerTests
{
    [Fact]
    public void AddEntitiesWritesInitialStateWithAttributes()
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act
        var ids = runtime.Entities.AddEntities("demo", [new DemoLight("living_ceiling", "天井照明", true, true, 204)]);

        // Assert
        var state = runtime.States.GetState("light.living_ceiling");
        Assert.Equal(["light.living_ceiling"], ids);
        Assert.NotNull(state);
        Assert.Equal(StateValues.On, state.State);
        Assert.Equal("天井照明", state.Name);
        Assert.Equal(204, state.GetAttribute(AttributeNames.Brightness));
        Assert.Equal(ColorModes.Brightness, state.GetAttribute(AttributeNames.ColorMode));
        Assert.Null(state.Context.UserId);
    }

    [Fact]
    public void EntityIdGetsSuffixWhenUsedAndFallsBackToPlatform()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.States.SetState("switch.demo", "on", null, EventContext.Create());

        // Act
        var ids = runtime.Entities.AddEntities("demo", [new NamedSwitch("テレビ"), new NamedSwitch("Living TV"), new NamedSwitch("Living TV")]);

        // Assert
        Assert.Equal(["switch.demo_2", "switch.living_tv", "switch.living_tv_2"], ids);
    }

    [Fact]
    public void DuplicateUniqueIdIsIgnored()
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act
        var ids = runtime.Entities.AddEntities("demo", [new DemoSwitch("tv", "テレビ", null, true), new DemoSwitch("tv", "テレビ", null, true)]);

        // Assert
        Assert.Single(ids);
        Assert.Equal(1, runtime.Entities.Count);
    }

    [Fact]
    public void UnavailableEntityWritesUnavailableWithoutStateAttributes()
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act
        runtime.Entities.AddEntities("test", [new UnavailableLight()]);

        // Assert
        var state = runtime.States.GetState("light.broken");
        Assert.Equal(StateValues.Unavailable, state?.State);
        Assert.NotNull(state?.GetAttribute(AttributeNames.SupportedColorModes));
        Assert.Null(state?.GetAttribute(AttributeNames.ColorMode));
    }

    [Fact]
    public void RemovePlatformRemovesStatesAndIgnoresLaterWrites()
    {
        // Arrange
        var runtime = new TestRuntime();
        var sensor = new DemoSensor("room", "室温", DeviceClasses.Temperature, "°C", 20, null);
        runtime.Entities.AddEntities("demo", [sensor, new DemoSwitch("tv", "テレビ", null, true)]);

        // Act
        var removed = runtime.Entities.RemovePlatform("demo");
        sensor.Update(25);

        // Assert
        Assert.Equal(2, removed);
        Assert.Equal(0, runtime.States.Count);
        Assert.Empty(runtime.Entities.ListEntities());
    }

    private sealed class NamedSwitch : SwitchEntity
    {
        public NamedSwitch(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        public override bool? IsOn => false;

        public override ValueTask TurnOnAsync(CancellationToken cancel) => ValueTask.CompletedTask;

        public override ValueTask TurnOffAsync(CancellationToken cancel) => ValueTask.CompletedTask;
    }

    private sealed class UnavailableLight : LightEntity
    {
        public override string SuggestedObjectId => "broken";

        public override bool Available => false;

        public override bool? IsOn => true;

        public override ValueTask TurnOnAsync(LightTurnOnArgs args, CancellationToken cancel) => ValueTask.CompletedTask;

        public override ValueTask TurnOffAsync(CancellationToken cancel) => ValueTask.CompletedTask;
    }
}

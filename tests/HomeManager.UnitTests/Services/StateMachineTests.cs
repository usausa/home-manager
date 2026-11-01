namespace HomeManager.Services;

using HomeManager.Domain;

public sealed class StateMachineTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private static readonly int[] Values = [1, 2];

    [Fact]
    public async Task SetNewStatePublishesStateChanged()
    {
        // Arrange
        var runtime = new TestRuntime();
        using var changed = new EventRecorder<StateChangedEvent>(runtime.Bus);
        var context = EventContext.Create("admin");

        // Act
        var state = runtime.States.SetState("sensor.room", "23.5", new Dictionary<string, object?> { ["unit_of_measurement"] = "°C" }, context);

        // Assert
        var value = await changed.NextAsync();
        Assert.Null(value.OldState);
        Assert.Same(state, value.NewState);
        Assert.Equal(TestRuntime.Start, state.LastChanged);
        Assert.Equal(TestRuntime.Start, state.LastUpdated);
        Assert.Equal("admin", state.Context.UserId);
        Assert.Equal("°C", state.GetAttribute("unit_of_measurement"));
    }

    [Fact]
    public async Task SameStateAndAttributesPublishesStateReported()
    {
        // Arrange
        var runtime = new TestRuntime();
        var first = runtime.States.SetState("sensor.room", "23.5", new Dictionary<string, object?> { ["values"] = Values }, EventContext.Create());
        using var reported = new EventRecorder<StateReportedEvent>(runtime.Bus);
        runtime.Time.Advance(Second);

        // Act
        var second = runtime.States.SetState("sensor.room", "23.5", new Dictionary<string, object?> { ["values"] = new List<int> { 1, 2 } }, EventContext.Create("other"));

        // Assert
        var value = await reported.NextAsync();
        Assert.Equal(first.LastReported, value.OldLastReported);
        Assert.Equal(first.LastChanged, second.LastChanged);
        Assert.Equal(first.LastUpdated, second.LastUpdated);
        Assert.Equal(TestRuntime.Start + Second, second.LastReported);
        Assert.Equal(first.Context, second.Context);
    }

    [Fact]
    public void AttributeChangeKeepsLastChanged()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.States.SetState("light.room", "on", new Dictionary<string, object?> { ["brightness"] = 100 }, EventContext.Create());
        runtime.Time.Advance(Second);

        // Act
        var state = runtime.States.SetState("light.room", "on", new Dictionary<string, object?> { ["brightness"] = 200 }, EventContext.Create());

        // Assert
        Assert.Equal(TestRuntime.Start, state.LastChanged);
        Assert.Equal(TestRuntime.Start + Second, state.LastUpdated);
    }

    [Fact]
    public void StateChangeAndForceUpdateUpdateLastChanged()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.States.SetState("light.room", "on", null, EventContext.Create());
        runtime.Time.Advance(Second);

        // Act
        var changed = runtime.States.SetState("light.room", "off", null, EventContext.Create());
        runtime.Time.Advance(Second);
        var forced = runtime.States.SetState("light.room", "off", null, EventContext.Create(), forceUpdate: true);

        // Assert
        Assert.Equal(TestRuntime.Start + Second, changed.LastChanged);
        Assert.Equal(TestRuntime.Start + (Second * 2), forced.LastChanged);
    }

    [Fact]
    public void AttributesAreCopied()
    {
        // Arrange
        var runtime = new TestRuntime();
        var attributes = new Dictionary<string, object?> { ["mode"] = "auto" };

        // Act
        var state = runtime.States.SetState("select.mode", "auto", attributes, EventContext.Create());
        attributes["mode"] = "manual";

        // Assert
        Assert.Equal("auto", state.GetAttribute("mode"));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("Light.room")]
    public void InvalidEntityIdThrows(string entityId)
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => runtime.States.SetState(entityId, "on", null, EventContext.Create()));
    }

    [Fact]
    public async Task RemovePublishesStateChangedWithoutNewState()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.States.SetState("switch.tv", "on", null, EventContext.Create());
        using var changed = new EventRecorder<StateChangedEvent>(runtime.Bus);

        // Act
        var removed = runtime.States.RemoveState("switch.tv", EventContext.Create());
        var missing = runtime.States.RemoveState("switch.tv", EventContext.Create());

        // Assert
        var value = await changed.NextAsync();
        Assert.True(removed);
        Assert.False(missing);
        Assert.Equal("on", value.OldState?.State);
        Assert.Null(value.NewState);
        Assert.Null(runtime.States.GetState("switch.tv"));
    }

    [Fact]
    public void GetStatesFiltersByDomain()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.States.SetState("light.a", "on", null, EventContext.Create());
        runtime.States.SetState("light.b", "off", null, EventContext.Create());
        runtime.States.SetState("binary_sensor.light", "off", null, EventContext.Create());

        // Act
        var lights = runtime.States.GetStates(Domains.Light);

        // Assert
        Assert.Equal(["light.a", "light.b"], lights.Select(static x => x.EntityId).Order(StringComparer.Ordinal));
        Assert.Equal(3, runtime.States.Count);
    }
}

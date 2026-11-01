namespace HomeManager.Services;

using System.Text.Json.Nodes;

using HomeManager.Domain;

public sealed class ServiceRegistryTests
{
    [Fact]
    public async Task CallInvokesHandlerAfterPublishingCallService()
    {
        // Arrange
        var runtime = new TestRuntime();
        ServiceCall? received = null;
        runtime.Services.Register(
            new ServiceDefinition("test", "echo") { HasTarget = true },
            (call, _) =>
            {
                received = call;
                return ValueTask.FromResult<object?>(call.GetText("message"));
            });
        using var called = new EventRecorder<ServiceCalledEvent>(runtime.Bus);
        var context = EventContext.Create("admin");

        // Act
        var response = await runtime.Services.CallAsync("test", "echo", new JsonObject { ["message"] = "hello" }, ServiceTarget.Of("light.a"), context, TestContext.Current.CancellationToken);

        // Assert
        var value = await called.NextAsync();
        Assert.Equal("hello", response);
        Assert.Equal("test", value.Domain);
        Assert.Equal("echo", value.Service);
        Assert.Same(context, value.Context);
        Assert.Equal(["light.a"], received?.Target.EntityIds);
        Assert.Same(context, received?.Context);
    }

    [Fact]
    public async Task UnknownServiceThrows()
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ServiceNotFoundException>(async () => await runtime.Services.CallAsync("test", "missing", null, null, EventContext.Create(), TestContext.Current.CancellationToken));
        Assert.Equal("missing", ex.Service);
    }

    [Fact]
    public void RegisterRejectsInvalidName()
    {
        // Arrange
        var runtime = new TestRuntime();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => runtime.Services.Register(new ServiceDefinition("Test", "echo"), static (_, _) => ValueTask.CompletedTask));
    }

    [Fact]
    public void RemoveAndListServices()
    {
        // Arrange
        var runtime = new TestRuntime();
        runtime.Services.Register(new ServiceDefinition("test", "b"), static (_, _) => ValueTask.CompletedTask);
        runtime.Services.Register(new ServiceDefinition("test", "a"), static (_, _) => ValueTask.CompletedTask);

        // Act
        var removed = runtime.Services.Remove("test", "b");

        // Assert
        Assert.True(removed);
        Assert.False(runtime.Services.Contains("test", "b"));
        Assert.Equal(["test.a"], runtime.Services.ListServices().Where(static x => x.Domain == "test").Select(static x => x.FullName));
        Assert.Contains(runtime.Services.ListServices(), static x => x.FullName == "light.turn_on");
    }
}

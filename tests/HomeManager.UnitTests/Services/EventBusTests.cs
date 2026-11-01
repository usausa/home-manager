namespace HomeManager.Services;

using HomeManager.Domain;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class EventBusTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PublishDeliversOnlyMatchingTypeAndFilter()
    {
        // Arrange
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        using var custom = new EventRecorder<CustomEvent>(bus, static x => x.EventType == "target");
        using var all = new EventRecorder<IHomeEvent>(bus);

        // Act
        bus.Publish(new CustomEvent("other", null, EventContext.Create(), Now));
        bus.Publish(new SystemEvent(EventTypes.HomeManagerStart, EventContext.Create(), Now));
        bus.Publish(new CustomEvent("target", null, EventContext.Create(), Now));

        // Assert
        Assert.Equal("target", (await custom.NextAsync()).EventType);
        Assert.Equal("other", (await all.NextAsync()).EventType);
        Assert.Equal(EventTypes.HomeManagerStart, (await all.NextAsync()).EventType);
        Assert.Equal("target", (await all.NextAsync()).EventType);
        Assert.False(custom.TryNext(out _));
    }

    [Fact]
    public async Task HandlerExceptionDoesNotStopSubscription()
    {
        // Arrange
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var received = new List<string>();
        var done = new TaskCompletionSource();
        using var subscription = bus.Subscribe<CustomEvent>((x, _) =>
        {
            if (x.EventType == "fail")
            {
                throw new InvalidOperationException("Test");
            }

            received.Add(x.EventType);
            if (x.EventType == "last")
            {
                done.SetResult();
            }

            return ValueTask.CompletedTask;
        });

        // Act
        bus.Publish(new CustomEvent("fail", null, EventContext.Create(), Now));
        bus.Publish(new CustomEvent("last", null, EventContext.Create(), Now));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["last"], received);
    }

    [Fact]
    public async Task BoundedSubscriptionDropsOldest()
    {
        // Arrange
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var gate = new TaskCompletionSource();
        var received = new List<string>();
        var done = new TaskCompletionSource();
        using var subscription = bus.Subscribe<CustomEvent>(
            async (x, _) =>
            {
                // 最初の1件で止めて、後ろを溢れさせる
                await gate.Task;
                received.Add(x.EventType);
                if (x.EventType == "e5")
                {
                    done.SetResult();
                }
            },
            null,
            new SubscriptionOptions { Capacity = 2 });

        // Act
        bus.Publish(new CustomEvent("e1", null, EventContext.Create(), Now));
        await Task.Delay(100, TestContext.Current.CancellationToken);
        for (var i = 2; i <= 5; i++)
        {
            bus.Publish(new CustomEvent($"e{i}", null, EventContext.Create(), Now));
        }

        gate.SetResult();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["e1", "e4", "e5"], received);
    }

    [Fact]
    public void DisposeRemovesSubscriber()
    {
        // Arrange
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var subscription = bus.Subscribe<CustomEvent>(static (_, _) => ValueTask.CompletedTask);

        // Act
        subscription.Dispose();
        subscription.Dispose();
        bus.Publish(new CustomEvent("after", null, EventContext.Create(), Now));

        // Assert
        Assert.Equal(0, bus.SubscriberCount);
    }
}

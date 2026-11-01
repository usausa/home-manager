namespace HomeManager.Components;

using HomeManager.Host.Components;

using Microsoft.Extensions.Time.Testing;

public sealed class RefreshThrottleTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    [Fact]
    public void FirstRequestRunsImmediatelyAndLaterRequestsAreMerged()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var count = 0;
        using var throttle = new RefreshThrottle(time, Interval, () =>
        {
            count++;
            return Task.CompletedTask;
        });

        // Act & Assert
        throttle.Request();
        Assert.Equal(1, count);

        throttle.Request();
        throttle.Request();
        Assert.Equal(1, count);

        time.Advance(Interval);
        Assert.Equal(2, count);

        time.Advance(Interval);
        Assert.Equal(2, count);

        throttle.Request();
        Assert.Equal(3, count);
    }

    [Fact]
    public void DisposeStopsPendingRefresh()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var count = 0;
        var throttle = new RefreshThrottle(time, Interval, () =>
        {
            count++;
            return Task.CompletedTask;
        });
        throttle.Request();
        throttle.Request();

        // Act
        throttle.Dispose();
        time.Advance(Interval);
        throttle.Request();

        // Assert
        Assert.Equal(1, count);
    }
}

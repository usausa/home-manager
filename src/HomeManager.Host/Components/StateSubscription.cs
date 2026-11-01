namespace HomeManager.Host.Components;

// 画面 (回線) ごとの状態の購読。状態が変わったら間引いて画面に知らせる
public sealed class StateSubscription : IDisposable
{
    // 合図だけ受け取れればよいので、溢れたら古いものを捨てる小さなキューにする
    private const int Capacity = 16;

    private readonly RefreshThrottle throttle;

    private readonly IDisposable subscription;

    // filterはentity_idで絞る (nullはすべて)
    public StateSubscription(IEventBus bus, TimeProvider timeProvider, TimeSpan interval, Func<string, bool>? filter, Func<Task> refresh)
    {
        throttle = new RefreshThrottle(timeProvider, interval, refresh);
        subscription = bus.Subscribe<StateChangedEvent>(
            (_, _) =>
            {
                throttle.Request();
                return ValueTask.CompletedTask;
            },
            filter is null ? null : x => filter(x.EntityId),
            new SubscriptionOptions { Capacity = Capacity });
    }

    public void Dispose()
    {
        subscription.Dispose();
        throttle.Dispose();
    }
}

namespace HomeManager.Services;

// イベントバス (HA の EventBus)。購読者ごとに専用のChannelと処理タスクを持ち、遅い購読者が他を巻き込まない
public interface IEventBus
{
    void Publish(IHomeEvent value);

    // 型が合い、filterを満たすイベントだけを受け取る。filterは発行側のスレッドで呼ぶため軽くする
    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, ValueTask> handler,
        Func<TEvent, bool>? filter = null,
        SubscriptionOptions? options = null)
        where TEvent : IHomeEvent;
}

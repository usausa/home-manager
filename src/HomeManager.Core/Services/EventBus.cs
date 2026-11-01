namespace HomeManager.Services;

using System.Threading.Channels;

public sealed class EventBus : IEventBus
{
    private readonly Lock sync = new();

    private readonly ILogger<EventBus> log;

    private ISubscriber[] subscribers = [];

    public EventBus(ILogger<EventBus> log)
    {
        this.log = log;
    }

    public int SubscriberCount => Volatile.Read(ref subscribers).Length;

    // 書き込みはTryWriteだけなので待たない。発行の順序は購読者ごとに保たれる
    public void Publish(IHomeEvent value)
    {
        foreach (var subscriber in Volatile.Read(ref subscribers))
        {
            subscriber.Post(value);
        }
    }

    public IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, ValueTask> handler,
        Func<TEvent, bool>? filter = null,
        SubscriptionOptions? options = null)
        where TEvent : IHomeEvent
    {
        var subscriber = new Subscriber<TEvent>(this, handler, filter, options?.Capacity);
        lock (sync)
        {
            Volatile.Write(ref subscribers, [.. subscribers, subscriber]);
        }

        subscriber.Start();
        return subscriber;
    }

    private void Unsubscribe(ISubscriber subscriber)
    {
        lock (sync)
        {
            Volatile.Write(ref subscribers, [.. subscribers.Where(x => !ReferenceEquals(x, subscriber))]);
        }
    }

    private interface ISubscriber
    {
        void Post(IHomeEvent value);
    }

    private sealed class Subscriber<TEvent> : ISubscriber, IDisposable
        where TEvent : IHomeEvent
    {
        private readonly CancellationTokenSource cts = new();

        private readonly EventBus bus;

        private readonly Func<TEvent, CancellationToken, ValueTask> handler;

        private readonly Func<TEvent, bool>? filter;

        private readonly Channel<TEvent> channel;

        private readonly CancellationToken cancel;

        private int disposed;

        public Subscriber(EventBus bus, Func<TEvent, CancellationToken, ValueTask> handler, Func<TEvent, bool>? filter, int? capacity)
        {
            this.bus = bus;
            this.handler = handler;
            this.filter = filter;
            channel = capacity is { } size
                ? Channel.CreateBounded<TEvent>(new BoundedChannelOptions(size) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true })
                : Channel.CreateUnbounded<TEvent>(new UnboundedChannelOptions { SingleReader = true });
            // 破棄後もトークンを参照できるように先に取り出しておく
            cancel = cts.Token;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }

            bus.Unsubscribe(this);
            cts.Cancel();
            channel.Writer.TryComplete();
            cts.Dispose();
        }

        // 呼び出し元 (画面の同期コンテキストなど) を引き継がないようにスレッドプールで回す
        public void Start() => _ = Task.Run(RunAsync, CancellationToken.None);

        public void Post(IHomeEvent value)
        {
            if (value is not TEvent typed)
            {
                return;
            }

            try
            {
                if ((filter is not null) && !filter(typed))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                bus.log.ErrorEventFilterException(value.EventType, ex);
                return;
            }

            channel.Writer.TryWrite(typed);
        }

        private async Task RunAsync()
        {
            try
            {
                await foreach (var value in channel.Reader.ReadAllAsync(cancel))
                {
                    try
                    {
                        await handler(value, cancel);
                    }
                    catch (Exception ex) when ((ex is not OperationCanceledException) || !cancel.IsCancellationRequested)
                    {
                        // 1件の失敗で購読を止めない
                        bus.log.ErrorEventHandlerException(value.EventType, ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Disposed
            }
        }
    }
}

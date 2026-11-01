namespace HomeManager;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using HomeManager.Domain;
using HomeManager.Services;

// イベントバスに流れたイベントを順に受け取る (購読は非同期のため、待って取り出す)
public sealed class EventRecorder<TEvent> : IDisposable
    where TEvent : IHomeEvent
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly Channel<TEvent> channel = Channel.CreateUnbounded<TEvent>();

    private readonly IDisposable subscription;

    public EventRecorder(IEventBus bus, Func<TEvent, bool>? filter = null)
    {
        subscription = bus.Subscribe<TEvent>(
            (x, _) =>
            {
                channel.Writer.TryWrite(x);
                return ValueTask.CompletedTask;
            },
            filter);
    }

    public void Dispose() => subscription.Dispose();

    public async Task<TEvent> NextAsync()
    {
        using var cts = new CancellationTokenSource(Timeout);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public bool TryNext([MaybeNullWhen(false)] out TEvent value) => channel.Reader.TryRead(out value);
}

namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 開発者ツールのイベント (SCR-203)。イベントを購読して流れを見る・任意の名前のイベントを発火する
public sealed partial class EventToolPage
{
    private const int MaxEvents = 50;

    private const int Capacity = 256;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

    private readonly Lock sync = new();

    private readonly List<ReceivedEvent> buffer = [];

    private List<ReceivedEvent> received = [];

    private int number;

    private string? fireType;

    private string? fireData;

    private string? listenType;

    private string? listenCustomType;

    private RefreshThrottle? throttle;

    private IDisposable? subscription;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required IEventBus EventBus { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required ServiceContextProvider ContextProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private bool Listening => subscription is not null;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        throttle = new RefreshThrottle(TimeProvider, RefreshInterval, () => InvokeAsync(() =>
        {
            lock (sync)
            {
                received = [.. buffer];
            }

            StateHasChanged();
        }));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            subscription?.Dispose();
            throttle?.Dispose();
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnFireClick()
    {
        var eventType = fireType?.Trim() ?? string.Empty;
        if ((eventType.Length == 0) || (eventType.Length > EventTypes.MaxLength))
        {
            Snackbar.AddError($"イベントの種別を {EventTypes.MaxLength} 文字以内で入力してください。");
            return;
        }

        if (EventTypes.Known.Contains(eventType))
        {
            Snackbar.AddError($"{eventType} はシステムのイベントのため発火できません。");
            return;
        }

        if (!JsonFormatter.TryParseObject(fireData, out var data, out var error))
        {
            Snackbar.AddError(error);
            return;
        }

        EventBus.Publish(new CustomEvent(eventType, data, ContextProvider.CreateEventContext(), TimeProvider.GetUtcNow()));
        Snackbar.AddSuccess($"{eventType} を発火しました。");
    }

    private void OnStartClick()
    {
        var type = String.IsNullOrWhiteSpace(listenCustomType) ? listenType : listenCustomType.Trim();
        subscription = EventBus.Subscribe<IHomeEvent>(
            OnEventReceived,
            String.IsNullOrEmpty(type) ? null : x => x.EventType == type,
            new SubscriptionOptions { Capacity = Capacity });
    }

    private void OnStopClick()
    {
        subscription?.Dispose();
        subscription = null;
    }

    private void OnClearClick()
    {
        lock (sync)
        {
            buffer.Clear();
            received = [];
        }
    }

    // 購読のタスクで呼ばれる (JSONにするのも画面のスレッドの外で行う)
    private ValueTask OnEventReceived(IHomeEvent value, CancellationToken cancel)
    {
        var json = JsonFormatter.Format(value);
        lock (sync)
        {
            number++;
            buffer.Insert(0, new ReceivedEvent(number, value.EventType, EntityFormatter.FormatTime(value.TimeFired), json));
            if (buffer.Count > MaxEvents)
            {
                buffer.RemoveAt(buffer.Count - 1);
            }
        }

        throttle?.Request();
        return ValueTask.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Model
    //--------------------------------------------------------------------------------

    private sealed record ReceivedEvent(int Number, string EventType, string Time, string Json);
}

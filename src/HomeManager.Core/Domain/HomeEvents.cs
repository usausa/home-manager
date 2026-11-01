namespace HomeManager.Domain;

using System.Text.Json.Nodes;

// 状態が変わった (追加はOldStateがnull、削除はNewStateがnull)
public sealed record StateChangedEvent(
    string EntityId,
    EntityState? OldState,
    EntityState? NewState,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.StateChanged;
}

// 値は同じで報告だけ来た (last_reportedだけが進む)
public sealed record StateReportedEvent(
    string EntityId,
    DateTimeOffset OldLastReported,
    EntityState NewState,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.StateReported;
}

// サービスが呼ばれた
public sealed record ServiceCalledEvent(
    string Domain,
    string Service,
    JsonObject? Data,
    ServiceTarget? Target,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.CallService;
}

public sealed record ServiceRegisteredEvent(
    string Domain,
    string Service,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.ServiceRegistered;
}

public sealed record ServiceRemovedEvent(
    string Domain,
    string Service,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.ServiceRemoved;
}

// インテグレーションを読み込んだ
public sealed record ComponentLoadedEvent(
    string Component,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent
{
    public string EventType => EventTypes.ComponentLoaded;
}

// 起動と停止 (homemanager_start / homemanager_started / homemanager_stop)
public sealed record SystemEvent(
    string EventType,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent;

// 任意の名前のイベント (開発者ツールやオートメーションから発火する)
public sealed record CustomEvent(
    string EventType,
    JsonObject? Data,
    EventContext Context,
    DateTimeOffset TimeFired) : IHomeEvent;

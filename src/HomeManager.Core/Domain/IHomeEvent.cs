namespace HomeManager.Domain;

// イベントバスに流すイベント (HA の Event)
public interface IHomeEvent
{
    string EventType { get; }

    EventContext Context { get; }

    DateTimeOffset TimeFired { get; }
}

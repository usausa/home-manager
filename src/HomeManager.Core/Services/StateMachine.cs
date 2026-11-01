namespace HomeManager.Services;

using System.Collections.Frozen;

public sealed class StateMachine : IStateMachine
{
    private readonly Lock sync = new();

    private readonly Dictionary<string, EntityState> states = new(StringComparer.Ordinal);

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    public StateMachine(IEventBus bus, TimeProvider timeProvider)
    {
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    public int Count
    {
        get
        {
            lock (sync)
            {
                return states.Count;
            }
        }
    }

    public EntityState? GetState(string entityId)
    {
        lock (sync)
        {
            return states.GetValueOrDefault(entityId);
        }
    }

    public IReadOnlyList<EntityState> GetStates(string? domain = null)
    {
        lock (sync)
        {
            return domain is null ? [.. states.Values] : [.. states.Values.Where(x => EntityIds.InDomain(x.EntityId, domain))];
        }
    }

    // 辞書の更新と発行を同じロックの中で行い、全購読者が同じ順序で受け取るようにする
    public EntityState SetState(
        string entityId,
        string state,
        IReadOnlyDictionary<string, object?>? attributes,
        EventContext context,
        bool forceUpdate = false)
    {
        if (!EntityIds.IsValid(entityId))
        {
            throw new ArgumentException($"Invalid entity id. entityId=[{entityId}]", nameof(entityId));
        }

        if (state.Length > StateValues.MaxLength)
        {
            throw new ArgumentException($"State is too long. entityId=[{entityId}]", nameof(state));
        }

        var newAttributes = Freeze(attributes);
        lock (sync)
        {
            var now = timeProvider.GetUtcNow();
            var lastChanged = now;
            if (states.TryGetValue(entityId, out var oldState) && !forceUpdate && (oldState.State == state))
            {
                if (AttributeComparer.AreEqual(oldState.Attributes, newAttributes))
                {
                    // 同じ値の報告は last_reported だけを進める (ContextはHAと同じく変えない)
                    var reportedState = oldState with { LastReported = now };
                    states[entityId] = reportedState;
                    bus.Publish(new StateReportedEvent(entityId, oldState.LastReported, reportedState, context, now));
                    return reportedState;
                }

                // 属性だけが変わったときは last_changed を引き継ぐ
                lastChanged = oldState.LastChanged;
            }

            var newState = new EntityState(entityId, state, newAttributes, lastChanged, now, now, context);
            states[entityId] = newState;
            bus.Publish(new StateChangedEvent(entityId, oldState, newState, context, now));
            return newState;
        }
    }

    public bool RemoveState(string entityId, EventContext context)
    {
        lock (sync)
        {
            if (!states.Remove(entityId, out var oldState))
            {
                return false;
            }

            bus.Publish(new StateChangedEvent(entityId, oldState, null, context, timeProvider.GetUtcNow()));
            return true;
        }
    }

    // 呼び出し元が後から辞書を書き換えても状態が変わらないように複製する
    private static FrozenDictionary<string, object?> Freeze(IReadOnlyDictionary<string, object?>? attributes) =>
        attributes switch
        {
            null or { Count: 0 } => FrozenDictionary<string, object?>.Empty,
            FrozenDictionary<string, object?> frozen => frozen,
            _ => attributes.ToFrozenDictionary(StringComparer.Ordinal)
        };
}

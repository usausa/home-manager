namespace HomeManager.Platforms;

// エンティティの基底 (HA の Entity)。インテグレーションはドメインごとの基底 (LightEntityなど) から派生する
public abstract class HomeEntity
{
    // 操作のContextを状態に引き継ぐ時間 (HA の CONTEXT_RECENT_TIME_SECONDS)
    private static readonly TimeSpan ContextRecentTime = TimeSpan.FromSeconds(5);

    private EventContext? context;

    private DateTimeOffset contextSetAt;

    private protected HomeEntity()
    {
    }

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    public abstract string Domain { get; }

    // EntityManagerに追加したときに決まる
    public string EntityId { get; internal set; } = string.Empty;

    // 追加したインテグレーションのドメイン
    public string Platform { get; internal set; } = string.Empty;

    public virtual string? UniqueId => null;

    // entity_idのobject_idの候補 (無ければ名前から作る)
    public virtual string? SuggestedObjectId => null;

    public virtual string? Name => null;

    public virtual bool Available => true;

    public virtual string? DeviceClass => null;

    public virtual string? UnitOfMeasurement => null;

    public virtual bool AssumedState => false;

    // nullはunknown
    public abstract string? State { get; }

    // インテグレーション固有の属性 (HA の extra_state_attributes)
    public virtual IReadOnlyDictionary<string, object?>? ExtraStateAttributes => null;

    internal EntityManager? Manager { get; set; }

    //--------------------------------------------------------------------------------
    // State
    //--------------------------------------------------------------------------------

    // 状態を書く (HA の async_write_ha_state)。追加前・削除後は何もしない
    protected void WriteState() => Manager?.WriteState(this);

    // ドメインの基底が決める属性 (HA の capability_attributes。利用不可でも書く)
    private protected virtual void CollectCapabilityAttributes(Dictionary<string, object?> attributes)
    {
    }

    // ドメインの基底が決める属性 (HA の state_attributes)
    private protected virtual void CollectStateAttributes(Dictionary<string, object?> attributes)
    {
    }

    internal Dictionary<string, object?> BuildAttributes(bool available)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        CollectCapabilityAttributes(attributes);
        if (available)
        {
            CollectStateAttributes(attributes);
            if (ExtraStateAttributes is { } extra)
            {
                foreach (var (key, value) in extra)
                {
                    attributes[key] = value;
                }
            }
        }

        if (UnitOfMeasurement is { } unit)
        {
            attributes[AttributeNames.UnitOfMeasurement] = unit;
        }

        if (DeviceClass is { } deviceClass)
        {
            attributes[AttributeNames.DeviceClass] = deviceClass;
        }

        if (AssumedState)
        {
            attributes[AttributeNames.AssumedState] = true;
        }

        if (Name is { } name)
        {
            attributes[AttributeNames.FriendlyName] = name;
        }

        return attributes;
    }

    //--------------------------------------------------------------------------------
    // Context
    //--------------------------------------------------------------------------------

    // サービスを呼んだ操作を覚え、直後に書く状態のContextにする (HA の async_set_context)
    internal void SetContext(EventContext value, DateTimeOffset now)
    {
        context = value;
        contextSetAt = now;
    }

    internal EventContext? GetRecentContext(DateTimeOffset now) =>
        (context is not null) && (now - contextSetAt <= ContextRecentTime) ? context : null;
}

namespace HomeManager.Services;

using System.Text.Json;

using HomeManager.Accessors;

// 全体設定 (HA の core_config)。DBから読んでメモリに持ち、変更は楽観ロックで保存する
public sealed class CoreConfigService : ICoreConfigService
{
    private const string Key = "core.config";

    private readonly Lock sync = new();

    private readonly ILogger<CoreConfigService> log;

    private readonly SettingAccessor settingAccessor;

    private readonly IEventBus bus;

    private readonly TimeProvider timeProvider;

    private Snapshot current = new(new CoreConfig(), TimeZones.Find(TimeZones.Default));

    public CoreConfigService(
        ILogger<CoreConfigService> log,
        SettingAccessor settingAccessor,
        IEventBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.settingAccessor = settingAccessor;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    public CoreConfig Config => current.Config;

    public TimeZoneInfo TimeZone => current.Zone;

    public async ValueTask LoadAsync(EventContext context, CancellationToken cancellationToken = default)
    {
        var entity = await settingAccessor.QueryByKeyAsync(Key, cancellationToken);
        if (entity is null)
        {
            // 初回は既定値を保存する (同時に起動しても重ならないよう、既にあれば何もしない)
            await settingAccessor.InsertAsync(Key, JsonSerializer.Serialize(new CoreConfig(), StoredJson.Options), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            entity = await settingAccessor.QueryByKeyAsync(Key, cancellationToken) ?? throw new InvalidOperationException($"Setting is not found. key=[{Key}]");
        }

        Apply(Deserialize(entity.Value) with { Version = entity.Version }, context);
    }

    public async ValueTask<DataWriteStatus> UpdateAsync(CoreConfig value, EventContext context, CancellationToken cancellationToken = default)
    {
        var errors = CoreConfigRules.Validate(value);
        if (errors.Count > 0)
        {
            throw new ServiceValidationException(errors[0]);
        }

        var count = await settingAccessor.UpdateAsync(Key, JsonSerializer.Serialize(value, StoredJson.Options), timeProvider.GetUtcNow().UtcDateTime, value.Version, cancellationToken);
        if (count == 0)
        {
            return DataWriteStatus.Conflict;
        }

        Apply(value with { Version = value.Version + 1 }, context);
        return DataWriteStatus.Success;
    }

    // 新しい版だけを反映する (同時に保存されても、メモリの値は一番新しい版になる)
    private void Apply(CoreConfig value, EventContext context)
    {
        CoreConfig previous;
        lock (sync)
        {
            previous = current.Config;
            if (value.Version < previous.Version)
            {
                return;
            }

            current = new Snapshot(value, TimeZones.Find(value.TimeZone));
        }

        // 起動時の最初の読み込みは変更として扱わない
        if (previous.Version == 0)
        {
            return;
        }

        var changes = new Dictionary<string, object?>(StringComparer.Ordinal);
        AddIfChanged(changes, nameof(CoreConfig.Name), previous.Name, value.Name);
        AddIfChanged(changes, nameof(CoreConfig.Latitude), previous.Latitude, value.Latitude);
        AddIfChanged(changes, nameof(CoreConfig.Longitude), previous.Longitude, value.Longitude);
        AddIfChanged(changes, nameof(CoreConfig.Elevation), previous.Elevation, value.Elevation);
        AddIfChanged(changes, nameof(CoreConfig.TimeZone), previous.TimeZone, value.TimeZone);
        AddIfChanged(changes, nameof(CoreConfig.UnitSystem), previous.UnitSystem, value.UnitSystem);
        AddIfChanged(changes, nameof(CoreConfig.Currency), previous.Currency, value.Currency);
        AddIfChanged(changes, nameof(CoreConfig.Country), previous.Country, value.Country);
        if (changes.Count > 0)
        {
            bus.Publish(new CoreConfigUpdatedEvent(changes, context, timeProvider.GetUtcNow()));
        }
    }

    // 形が違うJSONは既定値として読む (起動を止めない。次に保存すると正しい形で書き直す)
    private CoreConfig Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CoreConfig>(json, StoredJson.Options) ?? new CoreConfig();
        }
        catch (JsonException ex)
        {
            log.WarnSettingInvalid(Key, ex);
            return new CoreConfig();
        }
    }

    private static void AddIfChanged<T>(Dictionary<string, object?> changes, string name, T previous, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(previous, value))
        {
            changes[JsonNamingPolicy.SnakeCaseLower.ConvertName(name)] = value;
        }
    }

    private sealed record Snapshot(CoreConfig Config, TimeZoneInfo Zone);
}

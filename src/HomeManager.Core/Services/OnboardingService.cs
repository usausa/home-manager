namespace HomeManager.Services;

using System.Collections.Frozen;
using System.Text.Json;

using HomeManager.Accessors;

// 初回セットアップの進み具合 (HA の onboarding)。済んだ手順をSettingのキーcore.onboardingに保存する
public sealed class OnboardingService : IOnboardingService
{
    private const string Key = "core.onboarding";

    private readonly ILogger<OnboardingService> log;

    private readonly SettingAccessor settingAccessor;

    private readonly TimeProvider timeProvider;

    private FrozenSet<string> done = [];

    public OnboardingService(
        ILogger<OnboardingService> log,
        SettingAccessor settingAccessor,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.settingAccessor = settingAccessor;
        this.timeProvider = timeProvider;
    }

    public bool IsDone(string stepName) => done.Contains(stepName);

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        var entity = await settingAccessor.QueryByKeyAsync(Key, cancellationToken);
        var value = entity is null ? null : Deserialize(entity.Value);
        done = (value?.Done ?? []).ToFrozenSet(StringComparer.Ordinal);
    }

    public async ValueTask CompleteAsync(string stepName, CancellationToken cancellationToken = default)
    {
        if (done.Contains(stepName))
        {
            return;
        }

        var steps = done.Append(stepName).Order(StringComparer.Ordinal).ToList();
        await settingAccessor.UpsertAsync(Key, JsonSerializer.Serialize(new OnboardingData { Done = steps }, StoredJson.Options), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        done = steps.ToFrozenSet(StringComparer.Ordinal);
    }

    // 形が違うJSONは「済んだ手順なし」として読む (起動を止めない)
    private OnboardingData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<OnboardingData>(json, StoredJson.Options);
        }
        catch (JsonException ex)
        {
            log.WarnSettingInvalid(Key, ex);
            return null;
        }
    }

    private sealed class OnboardingData
    {
        public IReadOnlyList<string> Done { get; init; } = [];
    }
}

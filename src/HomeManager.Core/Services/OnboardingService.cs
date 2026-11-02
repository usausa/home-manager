namespace HomeManager.Services;

using System.Collections.Frozen;
using System.Text.Json;

using HomeManager.Accessors;

// 初回セットアップの進み具合 (HA の onboarding)。済んだ手順をcore.Settingのキーcore.onboardingに保存する
public sealed class OnboardingService : IOnboardingService
{
    private const string Key = "core.onboarding";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SettingAccessor settingAccessor;

    private readonly TimeProvider timeProvider;

    private FrozenSet<string> done = [];

    public OnboardingService(
        SettingAccessor settingAccessor,
        TimeProvider timeProvider)
    {
        this.settingAccessor = settingAccessor;
        this.timeProvider = timeProvider;
    }

    public bool IsDone(string stepName) => done.Contains(stepName);

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        var entity = await settingAccessor.QueryByKeyAsync(Key, cancellationToken);
        var value = entity is null ? null : JsonSerializer.Deserialize<OnboardingData>(entity.Value, JsonOptions);
        done = (value?.Done ?? []).ToFrozenSet(StringComparer.Ordinal);
    }

    public async ValueTask CompleteAsync(string stepName, CancellationToken cancellationToken = default)
    {
        if (done.Contains(stepName))
        {
            return;
        }

        var steps = done.Append(stepName).Order(StringComparer.Ordinal).ToList();
        await settingAccessor.UpsertAsync(Key, JsonSerializer.Serialize(new OnboardingData { Done = steps }, JsonOptions), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        done = steps.ToFrozenSet(StringComparer.Ordinal);
    }

    private sealed class OnboardingData
    {
        public IReadOnlyList<string> Done { get; init; } = [];
    }
}

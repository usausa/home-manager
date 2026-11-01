namespace HomeManager.Platforms;

// 選択肢から選ぶ (HA の SelectEntity。運転モードなど)
public abstract class SelectEntity : HomeEntity
{
    public sealed override string Domain => Domains.Select;

    // nullは不明
    public abstract string? CurrentOption { get; }

    public abstract IReadOnlyList<string> Options { get; }

    // 選択肢に無い値は不明として扱う (HAと同じ)
    public sealed override string? State =>
        (CurrentOption is { } current) && Options.Contains(current) ? current : null;

    public abstract ValueTask SelectOptionAsync(string value, CancellationToken cancel);

    private protected override void CollectCapabilityAttributes(Dictionary<string, object?> attributes)
    {
        attributes[AttributeNames.Options] = Options.ToArray();
    }
}

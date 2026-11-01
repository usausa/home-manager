namespace HomeManager.Services;

// サービスの説明 (画面のサービス選択と入力の手がかりに使う)
public sealed record ServiceDefinition(string Domain, string Service)
{
    public string Description { get; init; } = string.Empty;

    // ターゲット (エンティティ) を取るか
    public bool HasTarget { get; init; }

    // ターゲットにできるエンティティのドメイン。nullは種類を問わない
    public string? TargetDomain { get; init; }

    public IReadOnlyList<ServiceField> Fields { get; init; } = [];

    public string FullName => $"{Domain}.{Service}";
}

public sealed record ServiceField(string Name, string Description)
{
    public bool Required { get; init; }

    public string? Example { get; init; }
}

namespace HomeManager.Integrations;

// システム情報の画面に出すインテグレーションの状況
public sealed record IntegrationInfo(
    string Domain,
    string Name,
    IotClass IotClass,
    IntegrationStatus Status,
    int EntityCount,
    string? Error,
    DateTimeOffset? LoadedAt);

public enum IntegrationStatus
{
    NotLoaded,
    Loaded,
    SetupError
}

namespace HomeManager.Services;

// システム情報の画面 (SCR-362) に出す内容。DatabaseはDBにつながらないときnull
public sealed record SystemInfo(
    string Version,
    string Framework,
    string OperatingSystem,
    DateTimeOffset StartedAt,
    DatabaseInfo? Database,
    int EntityCount,
    int ServiceCount,
    IReadOnlyList<IntegrationInfo> Integrations);

// Sizeはバイト数
public sealed record DatabaseInfo(string ServerVersion, long Size);

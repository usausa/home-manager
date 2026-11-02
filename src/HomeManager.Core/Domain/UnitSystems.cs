namespace HomeManager.Domain;

// 単位系 (HA の unit_system と同じ値)
public static class UnitSystems
{
    public const string Metric = "metric";

    public const string UsCustomary = "us_customary";

    public static IReadOnlyList<string> All { get; } = [Metric, UsCustomary];
}

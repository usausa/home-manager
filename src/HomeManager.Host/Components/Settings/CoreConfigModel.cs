namespace HomeManager.Host.Components.Settings;

// 全体設定の入力欄の値 (CoreConfigは書き換えられないため、編集用に書き換えられる形にしたもの)
public sealed class CoreConfigModel
{
    public string? Name { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int Elevation { get; set; }

    public string? TimeZone { get; set; }

    public string UnitSystem { get; set; } = UnitSystems.Metric;

    public string? Currency { get; set; }

    public string? Country { get; set; }

    public int Version { get; set; }

    public static CoreConfigModel From(CoreConfig value) =>
        new()
        {
            Name = value.Name,
            Latitude = value.Latitude,
            Longitude = value.Longitude,
            Elevation = value.Elevation,
            TimeZone = value.TimeZone,
            UnitSystem = value.UnitSystem,
            Currency = value.Currency,
            Country = value.Country,
            Version = value.Version
        };

    // 前後の空白を除き、通貨と国は大文字にそろえる
    public CoreConfig ToConfig() =>
        new()
        {
            Name = Name?.Trim() ?? string.Empty,
            Latitude = Latitude,
            Longitude = Longitude,
            Elevation = Elevation,
            TimeZone = TimeZone?.Trim() ?? string.Empty,
            UnitSystem = UnitSystem,
            Currency = Currency?.Trim().ToUpperInvariant() ?? string.Empty,
            Country = Country?.Trim().ToUpperInvariant() ?? string.Empty,
            Version = Version
        };
}

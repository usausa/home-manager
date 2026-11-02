namespace HomeManager.Domain;

using System.Text.Json.Serialization;

// 全体設定 (HA の core_config)。core.Settingのキーcore.configにJSONで保存する。既定値は初回の起動で保存する値
public sealed record CoreConfig
{
    public string Name { get; init; } = "我が家";

    public double Latitude { get; init; } = 35.681236;

    public double Longitude { get; init; } = 139.767125;

    // 標高 (m)
    public int Elevation { get; init; }

    // IANAのタイムゾーンID
    public string TimeZone { get; init; } = TimeZones.Default;

    public string UnitSystem { get; init; } = UnitSystems.Metric;

    // ISO 4217の通貨コード
    public string Currency { get; init; } = "JPY";

    // ISO 3166-1の国コード (2文字)
    public string Country { get; init; } = "JP";

    // 楽観ロックの版 (JSONには入れない。0はまだDBから読んでいない)
    [JsonIgnore]
    public int Version { get; init; }
}

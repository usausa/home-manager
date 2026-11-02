namespace HomeManager.Domain;

using System.Text.RegularExpressions;

// 全体設定の入力の決まり。画面の入力チェックとサービスの両方で使う (誤りは画面にそのまま出すメッセージ、正しければnull)
public static partial class CoreConfigRules
{
    public const double MinLatitude = -90;

    public const double MaxLatitude = 90;

    public const double MinLongitude = -180;

    public const double MaxLongitude = 180;

    public const int MinElevation = -1000;

    public const int MaxElevation = 10000;

    public static string? ValidateName(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return "家の名前を入力してください。";
        }

        return value.Length > Length.Name ? String.Create(CultureInfo.InvariantCulture, $"家の名前は {Length.Name} 文字以内で入力してください。") : null;
    }

    public static string? ValidateLatitude(double value) =>
        value is >= MinLatitude and <= MaxLatitude ? null : "緯度は -90〜90 の範囲で入力してください。";

    public static string? ValidateLongitude(double value) =>
        value is >= MinLongitude and <= MaxLongitude ? null : "経度は -180〜180 の範囲で入力してください。";

    public static string? ValidateElevation(int value) =>
        value is >= MinElevation and <= MaxElevation ? null : "標高は -1000〜10000 m の範囲で入力してください。";

    public static string? ValidateTimeZone(string? value) =>
        TimeZones.IsValid(value) ? null : "タイムゾーンは一覧から選んでください（例: Asia/Tokyo）。";

    public static string? ValidateUnitSystem(string? value) =>
        (value is not null) && UnitSystems.All.Contains(value) ? null : "単位系は一覧から選んでください。";

    public static string? ValidateCurrency(string? value) =>
        (value is not null) && CurrencyPattern().IsMatch(value) ? null : "通貨は英大文字 3 文字で入力してください（例: JPY）。";

    public static string? ValidateCountry(string? value) =>
        (value is not null) && CountryPattern().IsMatch(value) ? null : "国は英大文字 2 文字で入力してください（例: JP）。";

    public static IReadOnlyList<string> Validate(CoreConfig value) =>
    [
        .. new[]
        {
            ValidateName(value.Name),
            ValidateLatitude(value.Latitude),
            ValidateLongitude(value.Longitude),
            ValidateElevation(value.Elevation),
            ValidateTimeZone(value.TimeZone),
            ValidateUnitSystem(value.UnitSystem),
            ValidateCurrency(value.Currency),
            ValidateCountry(value.Country)
        }.OfType<string>()
    ];

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryPattern();
}

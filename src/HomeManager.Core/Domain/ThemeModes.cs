namespace HomeManager.Domain;

// 画面のテーマ (autoはOSの設定に合わせる)
public static class ThemeModes
{
    public const string Light = "light";

    public const string Dark = "dark";

    public const string Auto = "auto";

    public static IReadOnlyList<string> All { get; } = [Light, Dark, Auto];
}

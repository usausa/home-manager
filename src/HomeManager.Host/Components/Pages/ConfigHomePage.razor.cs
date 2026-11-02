namespace HomeManager.Host.Components.Pages;

// 設定トップ (SCR-301)。各設定の画面へのメニュー。P2以降の設定 (インテグレーション・デバイス・エリアなど) はできたものから足す
public sealed partial class ConfigHomePage
{
    private static IReadOnlyList<ConfigMenu> Menus { get; } =
    [
        new("config/general", "🌍 全体設定", "家の名前・位置・タイムゾーン・単位系・通貨"),
        new("config/info", "ℹ️ システム情報", "バージョン・稼働時間・データベース・インテグレーション・接続中の回線")
    ];

    //--------------------------------------------------------------------------------
    // Model
    //--------------------------------------------------------------------------------

    private sealed record ConfigMenu(string Href, string Title, string Description);
}

namespace HomeManager.Domain;

// ユーザーごとの表示の設定。AccountのPreferencesにJSONで保存する
public sealed record AccountPreferences
{
    public string Theme { get; init; } = ThemeModes.Auto;
}

namespace HomeManager.Domain;

// 画面で扱うユーザーの情報 (パスワードのハッシュは含めない)。Versionは楽観ロックの版
public sealed record AccountProfile(
    long Id,
    string Name,
    string DisplayName,
    string Role,
    bool MustChangePassword,
    AccountPreferences Preferences,
    int Version);

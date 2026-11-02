namespace HomeManager.Domain;

// ユーザーの入力の決まり。画面の入力チェックとサービスの両方で使う (誤りは画面にそのまま出すメッセージ、正しければnull)
public static class AccountRules
{
    public static string? ValidateDisplayName(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return "表示名を入力してください。";
        }

        return value.Length > Length.Name ? String.Create(CultureInfo.InvariantCulture, $"表示名は {Length.Name} 文字以内で入力してください。") : null;
    }

    public static string? ValidatePassword(string? value)
    {
        if (String.IsNullOrEmpty(value) || (value.Length < Length.PasswordMin))
        {
            return String.Create(CultureInfo.InvariantCulture, $"パスワードは {Length.PasswordMin} 文字以上で入力してください。");
        }

        return value.Length > Length.Password ? String.Create(CultureInfo.InvariantCulture, $"パスワードは {Length.Password} 文字以内で入力してください。") : null;
    }
}

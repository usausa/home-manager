namespace HomeManager.Host.Components.Settings;

// ユーザーの操作の結果を画面に出す文言
public static class AccountMessages
{
    public static string GetPasswordError(PasswordChangeStatus status) => status switch
    {
        PasswordChangeStatus.WrongPassword => "今のパスワードが違います。",
        PasswordChangeStatus.InvalidPassword => String.Create(CultureInfo.InvariantCulture, $"新しいパスワードは {Length.PasswordMin} 文字以上で、今のパスワードと違うものにしてください。"),
        _ => "ユーザーが見つかりません。もう一度ログインしてください。"
    };

    public static string GetRoleName(string role) => role switch
    {
        Roles.Administrator => "管理者",
        _ => role
    };

    // 確認の欄が新しいパスワードと同じか
    public static string? ValidateConfirm(string? value, string? password)
    {
        if (String.IsNullOrEmpty(value))
        {
            return "確認のため、新しいパスワードをもう一度入力してください。";
        }

        return value == password ? null : "新しいパスワードと同じものを入力してください。";
    }
}

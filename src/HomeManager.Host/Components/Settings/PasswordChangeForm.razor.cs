namespace HomeManager.Host.Components.Settings;

using MudBlazor;

// パスワードの変更の入力欄 (初回セットアップとプロフィールで使う)。変更は使う側が行う
public sealed partial class PasswordChangeForm
{
    private MudForm? form;

    private string? confirmPassword;

    // 入力された今のパスワードと新しいパスワード
    public string? CurrentPassword { get; private set; }

    public string? NewPassword { get; private set; }

    // すべての入力欄を確かめる (正しければtrue)
    public async Task<bool> ValidateAsync()
    {
        if (form is null)
        {
            return false;
        }

        await form.ValidateAsync();
        return form.IsValid;
    }

    // 変えた後に入力欄を空にする
    public async Task ResetAsync()
    {
        CurrentPassword = null;
        NewPassword = null;
        confirmPassword = null;
        if (form is not null)
        {
            await form.ResetValidationAsync();
        }

        StateHasChanged();
    }

    private string? ValidateConfirm(string? value) => AccountMessages.ValidateConfirm(value, NewPassword);
}

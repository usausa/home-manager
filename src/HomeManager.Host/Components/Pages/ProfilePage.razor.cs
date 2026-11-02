namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Settings;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// プロフィール (SCR-003)。表示名・テーマ・パスワード。保存したら上部バーとテーマにすぐ反映する
public sealed partial class ProfilePage
{
    private MudForm? profileForm;

    private MudForm? passwordForm;

    private string? displayName;

    private string theme = ThemeModes.Auto;

    private string? currentPassword;

    private string? newPassword;

    private string? confirmPassword;

    private bool busy;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required UserSession Session { get; set; }

    [Inject]
    public required IAccountService AccountService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private bool Found => Session.Profile is not null;

    private string LoginName => Session.Profile?.Name ?? string.Empty;

    private string RoleName => AccountMessages.GetRoleName(Session.Profile?.Role ?? string.Empty);

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        await Session.LoadAsync();
        ResetFromSession();
    }

    private void ResetFromSession()
    {
        displayName = Session.Profile?.DisplayName;
        theme = Session.Theme;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private string? ValidateConfirm(string? value) => AccountMessages.ValidateConfirm(value, newPassword);

    private async Task OnProfileSaveAsync()
    {
        if ((profileForm is null) || (Session.Profile is null))
        {
            return;
        }

        await profileForm.ValidateAsync();
        if (!profileForm.IsValid)
        {
            return;
        }

        await SaveProfileAsync(Session.Profile with { DisplayName = displayName?.Trim() ?? string.Empty }, "表示名を保存しました。");
    }

    private async Task OnThemeChangedAsync(string value)
    {
        if (Session.Profile is null)
        {
            return;
        }

        theme = value;
        await SaveProfileAsync(Session.Profile with { Preferences = Session.Profile.Preferences with { Theme = value } }, "テーマを変えました。");
    }

    // 保存したプロフィールを読み直してほかの部品 (上部バー・テーマ) に知らせる
    private async Task SaveProfileAsync(AccountProfile value, string message)
    {
        busy = true;
        try
        {
            var status = await AccountService.UpdateProfileAsync(value);
            await Session.ReloadAsync();
            if (status == DataWriteStatus.Success)
            {
                // 編集中の表示名は残し、テーマは保存した値にそろえる
                theme = Session.Theme;
                Snackbar.AddSuccess(message);
            }
            else
            {
                ResetFromSession();
                Snackbar.AddWarning("ほかの画面でプロフィールが変わったため、最新の値を読み込みました。");
            }
        }
        catch (ServiceValidationException ex)
        {
            Snackbar.AddError(ex.Message);
        }
        finally
        {
            busy = false;
        }
    }

    private async Task OnPasswordChangeAsync()
    {
        if ((passwordForm is null) || (Session.Profile is null))
        {
            return;
        }

        await passwordForm.ValidateAsync();
        if (!passwordForm.IsValid)
        {
            return;
        }

        busy = true;
        try
        {
            var status = await AccountService.ChangePasswordAsync(Session.Profile.Name, currentPassword ?? string.Empty, newPassword ?? string.Empty);
            if (status != PasswordChangeStatus.Success)
            {
                Snackbar.AddError(AccountMessages.GetPasswordError(status));
                return;
            }

            await Session.ReloadAsync();
            currentPassword = null;
            newPassword = null;
            confirmPassword = null;
            await passwordForm.ResetValidationAsync();
            Snackbar.AddSuccess("パスワードを変えました。");
        }
        finally
        {
            busy = false;
        }
    }
}

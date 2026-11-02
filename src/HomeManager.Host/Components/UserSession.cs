namespace HomeManager.Host.Components;

using HomeManager.Host.Application;

using Microsoft.AspNetCore.Components.Authorization;

// 回線ごとのログイン中のユーザー (表示名・テーマ・初回セットアップが必要か)。プロフィールの変更を上部バーとテーマにすぐ反映する
public sealed class UserSession
{
    private readonly AuthenticationStateProvider authenticationStateProvider;

    private readonly IAccountService accountService;

    private readonly IOnboardingService onboardingService;

    private Task? loading;

    public UserSession(
        AuthenticationStateProvider authenticationStateProvider,
        IAccountService accountService,
        IOnboardingService onboardingService)
    {
        this.authenticationStateProvider = authenticationStateProvider;
        this.accountService = accountService;
        this.onboardingService = onboardingService;
    }

    public event EventHandler? Changed;

    // ログインしていなければnull
    public AccountProfile? Profile { get; private set; }

    public bool IsAdministrator { get; private set; }

    public string DisplayName => Profile?.DisplayName ?? string.Empty;

    public string Theme => Profile?.Preferences.Theme ?? ThemeModes.Auto;

    // 初期パスワードのまま、または管理者が全体設定をまだ確かめていない
    public bool IsOnboardingRequired =>
        (Profile is not null) &&
        (Profile.MustChangePassword || (IsAdministrator && !onboardingService.IsDone(OnboardingSteps.CoreConfig)));

    // 回線で最初の1回だけ読む (同時に呼ばれても読むのは1回)
    public Task LoadAsync() => loading ??= ReloadAsync();

    // 変更を保存した後に読み直し、表示中の部品に知らせる
    public async Task ReloadAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var identity = state.User.Identity;
        var name = (identity?.IsAuthenticated ?? false) ? identity.Name : null;

        IsAdministrator = state.User.IsInRole(Roles.Administrator);
        Profile = String.IsNullOrEmpty(name) ? null : await accountService.FindProfileAsync(name);
        loading ??= Task.CompletedTask;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

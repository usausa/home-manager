namespace HomeManager.Components;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Application;
using HomeManager.Host.Components;
using HomeManager.Services;

using Microsoft.Extensions.DependencyInjection;

using MudBlazor;

using NSubstitute;

// ログイン中のユーザー (UserSession) を使う画面のテスト。ユーザーとDBのサービスは代わりのものを使う
public abstract class SessionTestBase : MudBlazorTestBase
{
    protected SessionTestBase()
    {
        AccountService = Substitute.For<IAccountService>();
        Onboarding = Substitute.For<IOnboardingService>();
        Onboarding.IsDone(OnboardingSteps.CoreConfig).Returns(true);

        Services.AddSingleton(AccountService);
        Services.AddSingleton(Onboarding);
        Services.AddScoped<UserSession>();
    }

    protected IAccountService AccountService { get; }

    protected IOnboardingService Onboarding { get; }

    protected IReadOnlyList<string> SnackbarMessages =>
        [.. Services.GetRequiredService<ISnackbar>().ShownSnackbars.Select(static x => x.Message ?? string.Empty)];

    protected static AccountProfile CreateProfile(string name = "admin", string role = Roles.Administrator, bool mustChangePassword = false) =>
        new(1, name, name, role, mustChangePassword, new AccountPreferences(), 1);

    // ログインしたことにする (プロフィールはFindProfileAsyncが返す)
    protected void SignIn(AccountProfile profile)
    {
        var authorization = AddAuthorization();
        authorization.SetAuthorized(profile.Name);
        if (profile.Role == Roles.Administrator)
        {
            authorization.SetRoles(Roles.Administrator);
            authorization.SetPolicies(Policies.Administrator);
        }

        AccountService.FindProfileAsync(profile.Name, Arg.Any<CancellationToken>()).Returns(profile);
    }
}

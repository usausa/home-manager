namespace HomeManager.Services;

using HomeManager.Accessors;
using HomeManager.Domain;
using HomeManager.Host.Application.Context;
using HomeManager.Infrastructure.Security;

using Microsoft.Extensions.DependencyInjection;

public sealed class AccountServiceTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public AccountServiceTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    // 初期ユーザーは初回セットアップでパスワードを変える。変えた後は今と同じパスワードには変えられない
    [ContainerFact]
    public async Task InitialPasswordIsChanged()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<IAccountService>();
        var cancel = TestContext.Current.CancellationToken;
        using var scope = BeginScope();

        // Act
        var initial = await service.FindProfileAsync("admin", cancel);
        var wrong = await service.ChangePasswordAsync("admin", "wrong-password", "new-password", cancel);
        var success = await service.ChangePasswordAsync("admin", "admin", "new-password", cancel);
        var changed = await service.FindProfileAsync("admin", cancel);
        var same = await service.ChangePasswordAsync("admin", "new-password", "new-password", cancel);
        var authenticated = await service.AuthenticateAsync("admin", "new-password");
        var old = await service.AuthenticateAsync("admin", "admin");

        // Assert
        Assert.True(initial?.MustChangePassword);
        Assert.Equal(PasswordChangeStatus.WrongPassword, wrong);
        Assert.Equal(PasswordChangeStatus.Success, success);
        Assert.False(changed?.MustChangePassword);
        Assert.Equal(initial!.Version + 1, changed!.Version);
        Assert.Equal(PasswordChangeStatus.InvalidPassword, same);
        Assert.NotNull(authenticated);
        Assert.Null(old);
    }

    // 表示名とテーマは版が合うときだけ保存する (JSONBの読み書きも確かめる)
    [ContainerFact]
    public async Task ProfileUpdateUsesVersion()
    {
        // Arrange
        var service = factory.Services.GetRequiredService<IAccountService>();
        var accessor = factory.Services.GetRequiredService<AccountAccessor>();
        var passwordProvider = factory.Services.GetRequiredService<IPasswordProvider>();
        var cancel = TestContext.Current.CancellationToken;
        using var scope = BeginScope();
        await accessor.InsertAsync("machi", "machi", passwordProvider.Generate("password-1234"), "User", false, TimeProvider.System.GetUtcNow().UtcDateTime);
        var profile = (await service.FindProfileAsync("machi", cancel))!;

        // Act
        var updated = await service.UpdateProfileAsync(profile with { DisplayName = " まち ", Preferences = new AccountPreferences { Theme = ThemeModes.Dark } }, cancel);
        var stale = await service.UpdateProfileAsync(profile with { DisplayName = "古い値" }, cancel);
        var saved = (await service.FindProfileAsync("machi", cancel))!;

        // Assert
        Assert.Equal(ThemeModes.Auto, profile.Preferences.Theme);
        Assert.Equal(DataWriteStatus.Success, updated);
        Assert.Equal(DataWriteStatus.Conflict, stale);
        Assert.Equal("まち", saved.DisplayName);
        Assert.Equal(ThemeModes.Dark, saved.Preferences.Theme);
        Assert.Equal(profile.Version + 1, saved.Version);
    }

    private IDisposable BeginScope() =>
        factory.Services.GetRequiredService<ApplicationServiceContextProvider>().Begin(static () => new ServiceContext(TimeProvider.System.GetLocalNow(), "test"));
}

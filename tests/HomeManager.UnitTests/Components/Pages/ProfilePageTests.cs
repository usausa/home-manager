namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Pages;
using HomeManager.Services;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using NSubstitute;

public sealed class ProfilePageTests : SessionTestBase
{
    private const string NewPassword = "new-password";

    [Fact]
    public async Task ThemeChangeIsSavedAndReloaded()
    {
        // Arrange
        var profile = CreateProfile();
        SignIn(profile);
        AccountService.FindProfileAsync("admin", Arg.Any<CancellationToken>())
            .Returns(profile, profile with { Preferences = new AccountPreferences { Theme = ThemeModes.Dark }, Version = 2 });
        AccountService.UpdateProfileAsync(Arg.Any<AccountProfile>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Success);
        var cut = Render<ProfilePage>();

        // Act
        var group = cut.FindComponent<MudRadioGroup<string>>();
        await cut.InvokeAsync(() => group.Instance.ValueChanged.InvokeAsync(ThemeModes.Dark));

        // Assert
        await AccountService.Received(1).UpdateProfileAsync(Arg.Is<AccountProfile>(static x => x.Preferences.Theme == ThemeModes.Dark && x.Version == 1), Arg.Any<CancellationToken>());
        Assert.Contains("テーマを変えました。", SnackbarMessages);
        Assert.Equal(ThemeModes.Dark, group.Instance.Value);
    }

    [Fact]
    public async Task DisplayNameIsSaved()
    {
        // Arrange
        SignIn(CreateProfile());
        AccountService.UpdateProfileAsync(Arg.Any<AccountProfile>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Success);
        var cut = Render<ProfilePage>();

        // Act
        await cut.Find("input:not([type=password]):not([type=radio])").ChangeAsync(new ChangeEventArgs { Value = " まち " });
        ClickButton(cut, "保存");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("表示名を保存しました。", SnackbarMessages));
        await AccountService.Received(1).UpdateProfileAsync(Arg.Is<AccountProfile>(static x => x.DisplayName == "まち"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConflictReloadsProfile()
    {
        // Arrange
        SignIn(CreateProfile());
        AccountService.UpdateProfileAsync(Arg.Any<AccountProfile>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Conflict);
        var cut = Render<ProfilePage>();

        // Act
        cut.Find("input:not([type=password]):not([type=radio])").Change("まち");
        ClickButton(cut, "保存");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("ほかの画面でプロフィールが変わったため、最新の値を読み込みました。", SnackbarMessages));
        Assert.Equal("admin", cut.Find("input:not([type=password]):not([type=radio])").GetAttribute("value"));
    }

    [Fact]
    public async Task PasswordIsChanged()
    {
        // Arrange
        SignIn(CreateProfile());
        AccountService.ChangePasswordAsync("admin", "current-password", NewPassword, Arg.Any<CancellationToken>()).Returns(PasswordChangeStatus.Success);
        var cut = Render<ProfilePage>();

        // Act
        await InputPasswordsAsync(cut, "current-password", NewPassword, NewPassword);
        ClickButton(cut, "変更");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("パスワードを変えました。", SnackbarMessages));
        await AccountService.Received(1).ChangePasswordAsync("admin", "current-password", NewPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShortPasswordIsRejected()
    {
        // Arrange
        SignIn(CreateProfile());
        var cut = Render<ProfilePage>();

        // Act
        await InputPasswordsAsync(cut, "current-password", "short", "short");
        ClickButton(cut, "変更");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("パスワードは 8 文字以上で入力してください。", cut.Markup, StringComparison.Ordinal));
        await AccountService.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!, Xunit.TestContext.Current.CancellationToken);
    }

    private static async Task InputPasswordsAsync(IRenderedComponent<ProfilePage> cut, string current, string password, string confirm)
    {
        await cut.FindAll("input[type=password]")[0].ChangeAsync(new ChangeEventArgs { Value = current });
        await cut.FindAll("input[type=password]")[1].ChangeAsync(new ChangeEventArgs { Value = password });
        await cut.FindAll("input[type=password]")[2].ChangeAsync(new ChangeEventArgs { Value = confirm });
    }

    private static void ClickButton(IRenderedComponent<ProfilePage> cut, string text) =>
        cut.FindAll("button").Single(x => x.TextContent.Trim() == text).Click();
}

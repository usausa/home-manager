namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Pages;
using HomeManager.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

public sealed class OnboardingPageTests : SessionTestBase
{
    private const string NewPassword = "new-password";

    public OnboardingPageTests()
    {
        CoreConfig.UpdateAsync(Arg.Any<CoreConfig>(), Arg.Any<EventContext>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Success);
    }

    // 初期パスワードの変更 → 家の情報 → 完了 → 概要へ
    [Fact]
    public async Task PasswordAndCoreConfigStepsCompleteOnboarding()
    {
        // Arrange
        var profile = CreateProfile(mustChangePassword: true);
        SignIn(profile);
        AccountService.FindProfileAsync("admin", Arg.Any<CancellationToken>()).Returns(profile, profile with { MustChangePassword = false, Version = 2 });
        AccountService.ChangePasswordAsync("admin", "admin", NewPassword, Arg.Any<CancellationToken>()).Returns(PasswordChangeStatus.Success);
        Onboarding.IsDone(OnboardingSteps.CoreConfig).Returns(false);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<OnboardingPage>();

        // Act (パスワード)
        InputPasswords(cut, "admin", NewPassword, NewPassword);
        ClickButton(cut, "変更して次へ");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("家の情報", cut.Find("h6").TextContent, StringComparison.Ordinal));
        Assert.Equal(3, cut.FindAll(".mud-chip").Count);

        // Act (家の情報)
        ClickButton(cut, "保存して次へ");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("準備ができました", cut.Markup, StringComparison.Ordinal));
        await CoreConfig.Received(1).UpdateAsync(Arg.Is<CoreConfig>(static x => x.Name == "我が家" && x.Version == 1), Arg.Is<EventContext>(static x => x.UserId == "admin"), Arg.Any<CancellationToken>());
        await Onboarding.Received(1).CompleteAsync(OnboardingSteps.CoreConfig, Arg.Any<CancellationToken>());

        // Act (完了)
        ClickButton(cut, "はじめる");

        // Assert
        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }

    [Fact]
    public void WrongPasswordStaysOnPasswordStep()
    {
        // Arrange
        SignIn(CreateProfile(mustChangePassword: true));
        AccountService.ChangePasswordAsync("admin", "wrong", NewPassword, Arg.Any<CancellationToken>()).Returns(PasswordChangeStatus.WrongPassword);
        var cut = Render<OnboardingPage>();

        // Act
        InputPasswords(cut, "wrong", NewPassword, NewPassword);
        ClickButton(cut, "変更して次へ");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("今のパスワードが違います。", SnackbarMessages));
        Assert.Contains("パスワードの変更", cut.Find("h6").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmMismatchDoesNotChangePassword()
    {
        // Arrange
        SignIn(CreateProfile(mustChangePassword: true));
        var cut = Render<OnboardingPage>();

        // Act
        InputPasswords(cut, "admin", NewPassword, "other-password");
        ClickButton(cut, "変更して次へ");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("新しいパスワードと同じものを入力してください。", cut.Markup, StringComparison.Ordinal));
        await AccountService.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default!, default!, Xunit.TestContext.Current.CancellationToken);
    }

    // 管理者でなければ家の情報の手順は出さない
    [Fact]
    public void UserSkipsCoreConfigStep()
    {
        // Arrange
        SignIn(CreateProfile("machi", "User", mustChangePassword: true));
        Onboarding.IsDone(OnboardingSteps.CoreConfig).Returns(false);

        // Act
        var cut = Render<OnboardingPage>();

        // Assert
        Assert.Equal(2, cut.FindAll(".mud-chip").Count);
        Assert.DoesNotContain("家の情報", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NothingToDoGoesToOverview()
    {
        // Arrange
        SignIn(CreateProfile());
        var navigation = Services.GetRequiredService<NavigationManager>();

        // Act
        Render<OnboardingPage>();

        // Assert
        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }

    private static void InputPasswords(IRenderedComponent<OnboardingPage> cut, string current, string password, string confirm)
    {
        cut.FindAll("input[type=password]")[0].Change(current);
        cut.FindAll("input[type=password]")[1].Change(password);
        cut.FindAll("input[type=password]")[2].Change(confirm);
    }

    private static void ClickButton(IRenderedComponent<OnboardingPage> cut, string text) =>
        cut.FindAll("button").Single(x => x.TextContent.Contains(text, StringComparison.Ordinal)).Click();
}

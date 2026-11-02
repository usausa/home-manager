namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Pages;
using HomeManager.Services;

using Microsoft.AspNetCore.Components;

using NSubstitute;

public sealed class GeneralSettingsPageTests : SessionTestBase
{
    public GeneralSettingsPageTests()
    {
        SignIn(CreateProfile());
    }

    [Fact]
    public async Task SaveUpdatesCoreConfig()
    {
        // Arrange
        CoreConfig.UpdateAsync(Arg.Any<CoreConfig>(), Arg.Any<EventContext>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Success);
        var cut = Render<GeneralSettingsPage>();

        // Act
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "別荘" });
        ClickButton(cut, "保存");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("全体設定を保存しました。", SnackbarMessages));
        await CoreConfig.Received(1).UpdateAsync(Arg.Is<CoreConfig>(static x => x.Name == "別荘" && x.Version == 1), Arg.Is<EventContext>(static x => x.UserId == "admin"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConflictReloadsLatestValue()
    {
        // Arrange
        CoreConfig.UpdateAsync(Arg.Any<CoreConfig>(), Arg.Any<EventContext>(), Arg.Any<CancellationToken>()).Returns(DataWriteStatus.Conflict);
        var cut = Render<GeneralSettingsPage>();

        // Act
        cut.Find("input").Change("別荘");
        ClickButton(cut, "保存");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("ほかの画面で全体設定が変わったため、最新の値を読み込みました。確かめてからもう一度保存してください。", SnackbarMessages));
        Assert.Equal("我が家", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task InvalidValueIsNotSaved()
    {
        // Arrange
        var cut = Render<GeneralSettingsPage>();

        // Act
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = " " });
        ClickButton(cut, "保存");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("家の名前を入力してください。", cut.Markup, StringComparison.Ordinal));
        await CoreConfig.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, Xunit.TestContext.Current.CancellationToken);
    }

    private static void ClickButton(IRenderedComponent<GeneralSettingsPage> cut, string text) =>
        cut.FindAll("button").Single(x => x.TextContent.Trim() == text).Click();
}

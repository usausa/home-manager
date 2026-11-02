namespace HomeManager.Components.Pages;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Pages;
using HomeManager.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MudBlazor;

public sealed class StateToolPageTests : MudBlazorTestBase
{
    private readonly TestRuntime runtime = new();

    public StateToolPageTests()
    {
        Services.AddSingleton<IEventBus>(runtime.Bus);
        Services.AddSingleton<IStateMachine>(runtime.States);
        AddAuthorization().SetAuthorized("admin");
    }

    [Fact]
    public async Task SetStateWritesState()
    {
        // Arrange
        var cut = Render<StateToolPage>();

        // Act
        await cut.FindAll("input")[0].ChangeAsync(new ChangeEventArgs { Value = "sensor.test" });
        await cut.FindAll("input")[1].ChangeAsync(new ChangeEventArgs { Value = "12.5" });
        ClickButton(cut, "設定");

        // Assert
        cut.WaitForAssertion(() => Assert.Equal("12.5", runtime.States.GetState("sensor.test")?.State));
        Assert.Equal("admin", runtime.States.GetState("sensor.test")?.Context.UserId);
    }

    // 状態は 255 文字まで (超えたら画面で止め、エラーの画面にしない)
    [Fact]
    public async Task TooLongStateIsRejected()
    {
        // Arrange
        var cut = Render<StateToolPage>();

        // Act
        await cut.FindAll("input")[0].ChangeAsync(new ChangeEventArgs { Value = "sensor.test" });
        await cut.FindAll("input")[1].ChangeAsync(new ChangeEventArgs { Value = new string('x', StateValues.MaxLength + 1) });
        ClickButton(cut, "設定");

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("状態は 255 文字以内で入力してください。", Services.GetRequiredService<ISnackbar>().ShownSnackbars.Select(static x => x.Message)));
        Assert.Null(runtime.States.GetState("sensor.test"));
    }

    private static void ClickButton(IRenderedComponent<StateToolPage> cut, string text) =>
        cut.FindAll("button").Single(x => x.TextContent.Trim() == text).Click();
}

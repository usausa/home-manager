namespace HomeManager;

using System.Text.RegularExpressions;

using Microsoft.Playwright;

public sealed class LoginTests : E2ETestBase
{
    // 初回は初期パスワードの変更と家の情報の確認を済ませてから概要に進む
    [ContainerFact]
    public async Task FirstLoginGoesThroughOnboarding()
    {
        // Given
        await using var factory = new E2EApplicationFactory();
        await factory.InitializeAsync();
        factory.UseKestrel(0);
        factory.StartServer();

        // When
        await Page.GotoAsync(factory.ServerAddress + "/");
        await Expect(Page).ToHaveURLAsync(new Regex(".*login.*"));

        await Page.FillAsync("#name", "admin");
        await Page.FillAsync("#password", "admin");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/onboarding$"));
        var passwords = Page.Locator("input[type=password]");
        await passwords.Nth(0).FillAsync("admin");
        await passwords.Nth(1).FillAsync("new-password");
        await passwords.Nth(2).FillAsync("new-password");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "変更して次へ" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "保存して次へ" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "はじめる" }).ClickAsync();

        // Then
        await Expect(Page.GetByText("🏠 我が家")).ToBeVisibleAsync();
        await Expect(Page.GetByText("HomeManager")).ToBeVisibleAsync();
    }

    [ContainerFact]
    public async Task LoginWithWrongPasswordShowsError()
    {
        // Given
        await using var factory = new E2EApplicationFactory();
        await factory.InitializeAsync();
        factory.UseKestrel(0);
        factory.StartServer();

        // When
        await Page.GotoAsync(factory.ServerAddress + "/login");
        await Page.FillAsync("#name", "admin");
        await Page.FillAsync("#password", "wrong");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();

        // Then
        await Expect(Page).ToHaveURLAsync(new Regex(".*error=1.*"));
        await Expect(Page.Locator(".mud-alert")).ToBeVisibleAsync();
    }
}

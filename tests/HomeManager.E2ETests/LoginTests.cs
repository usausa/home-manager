namespace HomeManager;

using System.Text.RegularExpressions;

using Microsoft.Playwright;

public sealed class LoginTests : E2ETestBase
{
    [ContainerFact]
    public async Task LoginShowsHomePage()
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

        // Then
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

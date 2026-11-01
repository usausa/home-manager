namespace HomeManager.Components.Layout;

using Bunit;

using HomeManager.Host.Application;
using HomeManager.Host.Components.Layout;

public sealed class NavMenuTests : MudBlazorTestBase
{
    [Fact]
    public void AdministratorSeesDeveloperTools()
    {
        // Arrange
        var authorization = AddAuthorization();
        authorization.SetAuthorized("admin");
        authorization.SetPolicies(Policies.Administrator);

        // Act
        var cut = Render<NavMenu>();

        // Assert
        var links = cut.FindAll("a");
        Assert.Equal(4, links.Count);
        Assert.Contains(links, static x => x.GetAttribute("href") == "developer-tools/state");
    }

    [Fact]
    public void UserSeesOverviewOnly()
    {
        // Arrange
        AddAuthorization().SetAuthorized("user");

        // Act
        var cut = Render<NavMenu>();

        // Assert
        Assert.Single(cut.FindAll("a"));
    }
}

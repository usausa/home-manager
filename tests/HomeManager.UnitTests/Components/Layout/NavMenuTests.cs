namespace HomeManager.Components.Layout;

using Bunit;

using HomeManager.Host.Components.Layout;

public sealed class NavMenuTests : MudBlazorTestBase
{
    [Fact]
    public void RenderShowsNavigationLinks()
    {
        // Arrange & Act
        var cut = Render<NavMenu>();

        // Assert
        var links = cut.FindAll("a");
        Assert.Single(links);
    }
}

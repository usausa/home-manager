namespace HomeManager.Components.Cards;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Cards;

public sealed class EntityTileTests : MudBlazorTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly EntityState Light = new(
        "light.ceiling",
        StateValues.On,
        new Dictionary<string, object?>
        {
            [AttributeNames.FriendlyName] = "天井照明",
            [AttributeNames.Brightness] = 128
        },
        Now,
        Now,
        Now,
        EventContext.Create());

    [Fact]
    public void RenderShowsNameAndState()
    {
        // Act
        var cut = Render<EntityTile>(parameters => parameters.Add(static x => x.State, Light));

        // Assert
        Assert.Contains("天井照明", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("オン・50%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("entity-tile-active", cut.Find(".entity-tile").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void IconClickActsAndTileClickOpens()
    {
        // Arrange
        EntityState? action = null;
        EntityState? open = null;
        var cut = Render<EntityTile>(parameters => parameters
            .Add(static x => x.State, Light)
            .Add(x => x.OnAction, x => action = x)
            .Add(x => x.OnOpen, x => open = x));

        // Act
        cut.Find("button").Click();
        var openedByIcon = open;
        cut.Find(".entity-tile").Click();

        // Assert
        Assert.Same(Light, action);
        Assert.Null(openedByIcon);
        Assert.Same(Light, open);
    }
}

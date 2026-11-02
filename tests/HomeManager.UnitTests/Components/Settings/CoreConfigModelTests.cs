namespace HomeManager.Components.Settings;

using HomeManager.Domain;
using HomeManager.Host.Components.Settings;

public sealed class CoreConfigModelTests
{
    [Fact]
    public void ToConfigTrimsAndNormalizes()
    {
        // Arrange
        var model = CoreConfigModel.From(new CoreConfig { Version = 3 });
        model.Name = "  別荘 ";
        model.Currency = " usd";
        model.Country = "us ";

        // Act
        var value = model.ToConfig();

        // Assert
        Assert.Equal("別荘", value.Name);
        Assert.Equal("USD", value.Currency);
        Assert.Equal("US", value.Country);
        Assert.Equal(TimeZones.Default, value.TimeZone);
        Assert.Equal(3, value.Version);
    }
}

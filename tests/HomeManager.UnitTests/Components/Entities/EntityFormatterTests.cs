namespace HomeManager.Components.Entities;

using HomeManager.Domain;
using HomeManager.Host.Components.Entities;

using MudBlazor;

public sealed class EntityFormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("light.ceiling", "on", null, null, "オン")]
    [InlineData("light.ceiling", "off", null, null, "オフ")]
    [InlineData("binary_sensor.hall", "on", "motion", null, "検知")]
    [InlineData("binary_sensor.hall", "off", "motion", null, "なし")]
    [InlineData("binary_sensor.door", "on", "door", null, "開")]
    [InlineData("sensor.room", "23.5", "temperature", "°C", "23.5 °C")]
    [InlineData("sensor.room", "48", "humidity", "%", "48%")]
    [InlineData("switch.tv", "unavailable", null, null, "利用不可")]
    [InlineData("select.mode", "unknown", null, null, "不明")]
    public void FormatStateUsesDomainAndDeviceClass(string entityId, string value, string? deviceClass, string? unit, string expected)
    {
        // Arrange
        var attributes = new Dictionary<string, object?>();
        if (deviceClass is not null)
        {
            attributes[AttributeNames.DeviceClass] = deviceClass;
        }

        if (unit is not null)
        {
            attributes[AttributeNames.UnitOfMeasurement] = unit;
        }

        var state = new EntityState(entityId, value, attributes, Now, Now, Now, EventContext.Create());

        // Act
        var text = EntityFormatter.FormatState(state);

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void LightShowsBrightnessPercentAndActiveColor()
    {
        // Arrange
        var state = new EntityState("light.ceiling", "on", new Dictionary<string, object?> { [AttributeNames.Brightness] = 128 }, Now, Now, Now, EventContext.Create());

        // Act & Assert
        Assert.Equal("オン・50%", EntityFormatter.FormatState(state));
        Assert.Equal(Icons.Material.Filled.Lightbulb, EntityFormatter.GetIcon(state));
        Assert.Equal(Color.Warning, EntityFormatter.GetColor(state));
    }

    [Fact]
    public void FormatRelativeAndUser()
    {
        // Act & Assert
        Assert.Equal("たった今", EntityFormatter.FormatRelative(Now, Now.AddSeconds(30)));
        Assert.Equal("3 分前", EntityFormatter.FormatRelative(Now, Now.AddMinutes(3)));
        Assert.Equal("2 時間前", EntityFormatter.FormatRelative(Now, Now.AddHours(2)));
        Assert.Equal("admin", EntityFormatter.FormatUser(EventContext.Create("admin")));
        Assert.Equal("機器・システム", EntityFormatter.FormatUser(EventContext.Create()));
    }
}

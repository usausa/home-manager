namespace HomeManager.Domain;

public sealed class EntityIdsTests
{
    [Theory]
    [InlineData("light.living_ceiling", true)]
    [InlineData("sensor.co2_1", true)]
    [InlineData("light", false)]
    [InlineData("Light.ceiling", false)]
    [InlineData("light._ceiling", false)]
    [InlineData("light.ceiling_", false)]
    [InlineData("light.ceiling__room", false)]
    [InlineData("light.天井", false)]
    public void IsValidFollowsHomeAssistantRule(string entityId, bool expected)
    {
        // Act
        var valid = EntityIds.IsValid(entityId);

        // Assert
        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData("Living Room Light", "living_room_light")]
    [InlineData("  CO2 (ppm) ", "co2_ppm")]
    [InlineData("天井照明", "")]
    [InlineData("照明 2", "2")]
    [InlineData(null, "")]
    public void SlugifyKeepsAsciiLettersAndDigits(string? value, string expected)
    {
        // Act
        var slug = EntityIds.Slugify(value);

        // Assert
        Assert.Equal(expected, slug);
    }

    [Fact]
    public void DomainAndObjectIdAreSplitAtFirstDot()
    {
        // Act & Assert
        Assert.Equal("binary_sensor", EntityIds.GetDomain("binary_sensor.front_door"));
        Assert.Equal("front_door", EntityIds.GetObjectId("binary_sensor.front_door"));
        Assert.True(EntityIds.InDomain("binary_sensor.front_door", "binary_sensor"));
        Assert.False(EntityIds.InDomain("binary_sensor.front_door", "binary"));
    }
}

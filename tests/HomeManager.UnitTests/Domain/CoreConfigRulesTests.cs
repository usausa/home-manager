namespace HomeManager.Domain;

public sealed class CoreConfigRulesTests
{
    [Fact]
    public void DefaultConfigIsValid()
    {
        // Act
        var errors = CoreConfigRules.Validate(new CoreConfig());

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidValuesAreReported()
    {
        // Arrange
        var value = new CoreConfig
        {
            Name = " ",
            Latitude = 91,
            Longitude = -181,
            Elevation = 20000,
            TimeZone = "Tokyo Standard Time",
            UnitSystem = "imperial",
            Currency = "jpy",
            Country = "JPN"
        };

        // Act
        var errors = CoreConfigRules.Validate(value);

        // Assert
        Assert.Equal(8, errors.Count);
        Assert.Contains("家の名前を入力してください。", errors);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    public void LatitudeOutOfRangeIsInvalid(double value)
    {
        // Act & Assert
        Assert.NotNull(CoreConfigRules.ValidateLatitude(value));
    }

    [Fact]
    public void NameLongerThanLimitIsInvalid()
    {
        // Act & Assert
        Assert.Null(CoreConfigRules.ValidateName(new string('家', Length.Name)));
        Assert.NotNull(CoreConfigRules.ValidateName(new string('家', Length.Name + 1)));
    }
}

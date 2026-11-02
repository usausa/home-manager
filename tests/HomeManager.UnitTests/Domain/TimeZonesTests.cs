namespace HomeManager.Domain;

public sealed class TimeZonesTests
{
    // WindowsでもIANAのIDで一覧にする
    [Fact]
    public void ListContainsIanaIds()
    {
        // Act & Assert
        Assert.Contains("Asia/Tokyo", TimeZones.All);
        Assert.DoesNotContain("Tokyo Standard Time", TimeZones.All);
        Assert.True(TimeZones.IsValid("Asia/Tokyo"));
        Assert.False(TimeZones.IsValid("Tokyo Standard Time"));
        Assert.False(TimeZones.IsValid(null));
    }

    [Fact]
    public void FindFallsBackToUtc()
    {
        // Act & Assert
        Assert.Equal(TimeSpan.FromHours(9), TimeZones.Find("Asia/Tokyo").BaseUtcOffset);
        Assert.Same(TimeZoneInfo.Utc, TimeZones.Find("Unknown/Zone"));
    }
}

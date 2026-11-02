namespace HomeManager.Components.Settings;

using HomeManager.Host.Components.Settings;

public sealed class SystemFormatterTests
{
    [Theory]
    [InlineData("0.0.1+4b560815f3a07bf21ea63eec5ad57d9b4547dc28", "0.0.1（4b56081）")]
    [InlineData("0.0.1+abc", "0.0.1（abc）")]
    [InlineData("0.0.1", "0.0.1")]
    public void FormatVersionShortensCommit(string value, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, SystemFormatter.FormatVersion(value));
    }

    [Fact]
    public void FormatDurationUsesLargestUnits()
    {
        // Act & Assert
        Assert.Equal("0 分 5 秒", SystemFormatter.FormatDuration(TimeSpan.FromSeconds(5)));
        Assert.Equal("2 時間 3 分 4 秒", SystemFormatter.FormatDuration(new TimeSpan(2, 3, 4)));
        Assert.Equal("1 日 2 時間 3 分", SystemFormatter.FormatDuration(new TimeSpan(1, 2, 3, 4)));
        Assert.Equal("0 分 0 秒", SystemFormatter.FormatDuration(TimeSpan.FromSeconds(-1)));
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(8 * 1024 * 1024, "8.0 MB")]
    public void FormatBytesUsesBinaryUnits(long value, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, SystemFormatter.FormatBytes(value));
    }
}

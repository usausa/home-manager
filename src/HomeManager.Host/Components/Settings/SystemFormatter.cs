namespace HomeManager.Host.Components.Settings;

// システム情報の表示 (版・期間・大きさ)
public static class SystemFormatter
{
    private const int CommitLength = 7;

    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    // 版の後ろのコミット (+4b560815f3...) は先頭7文字だけを括弧で添える
    public static string FormatVersion(string value)
    {
        var index = value.IndexOf('+', StringComparison.Ordinal);
        if (index < 0)
        {
            return value;
        }

        var commit = value[(index + 1)..];
        return $"{value[..index]}（{(commit.Length > CommitLength ? commit[..CommitLength] : commit)}）";
    }

    public static string FormatDuration(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        if (value.TotalDays >= 1)
        {
            return String.Create(CultureInfo.InvariantCulture, $"{(int)value.TotalDays} 日 {value.Hours} 時間 {value.Minutes} 分");
        }

        return value.TotalHours >= 1
            ? String.Create(CultureInfo.InvariantCulture, $"{value.Hours} 時間 {value.Minutes} 分 {value.Seconds} 秒")
            : String.Create(CultureInfo.InvariantCulture, $"{value.Minutes} 分 {value.Seconds} 秒");
    }

    public static string FormatBytes(long value)
    {
        var size = (double)value;
        var unit = 0;
        while ((size >= 1024) && (unit < Units.Length - 1))
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? String.Create(CultureInfo.InvariantCulture, $"{value} B")
            : String.Create(CultureInfo.InvariantCulture, $"{size:0.0} {Units[unit]}");
    }
}

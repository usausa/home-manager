namespace HomeManager.Domain;

// タイムゾーンはIANAのID (Asia/Tokyo など) で扱う。WindowsのIDはIANAのIDに変換して一覧にする
public static class TimeZones
{
    public const string Default = "Asia/Tokyo";

    private static readonly Lazy<string[]> Ids = new(LoadIds);

    public static IReadOnlyList<string> All => Ids.Value;

    public static bool IsValid(string? id) =>
        !String.IsNullOrEmpty(id) && (Array.BinarySearch(Ids.Value, id, StringComparer.Ordinal) >= 0);

    // 見つからなければUTCにする
    public static TimeZoneInfo Find(string? id) =>
        IsValid(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id!, out var zone) ? zone : TimeZoneInfo.Utc;

    private static string[] LoadIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in TimeZoneInfo.GetSystemTimeZones())
        {
            if (zone.HasIanaId)
            {
                ids.Add(zone.Id);
            }
            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId))
            {
                ids.Add(ianaId);
            }
        }

        var array = ids.ToArray();
        Array.Sort(array, StringComparer.Ordinal);
        return array;
    }
}

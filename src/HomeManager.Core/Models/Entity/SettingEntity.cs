namespace HomeManager.Models.Entity;

public sealed class SettingEntity
{
    public string Key { get; set; } = default!;

    // JSONは文字列で受け取る
    public string Value { get; set; } = default!;

    public DateTime UpdatedAt { get; set; }

    public int Version { get; set; }
}

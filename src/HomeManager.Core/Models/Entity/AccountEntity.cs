namespace HomeManager.Models.Entity;

public sealed class AccountEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

#pragma warning disable CA1819
    public byte[] Password { get; set; } = default!;
#pragma warning restore CA1819

    public string Role { get; set; } = default!;

    public bool IsEnabled { get; set; }

    public bool MustChangePassword { get; set; }

    // JSONBはJSONの文字列で受け取る
    public string Preferences { get; set; } = default!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int Version { get; set; }
}

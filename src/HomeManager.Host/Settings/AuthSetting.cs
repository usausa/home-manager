namespace HomeManager.Host.Settings;

public sealed class AuthSetting
{
    [Range(1, 43200)]
    public int ExpireMinutes { get; set; }

    [Required]
    public InitialAccountOption InitialAccount { get; set; } = default!;
}

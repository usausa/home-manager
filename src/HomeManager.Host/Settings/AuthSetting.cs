namespace HomeManager.Host.Settings;

using Microsoft.Extensions.Options;

public sealed class AuthSetting
{
    [Range(1, 43200)]
    public int ExpireMinutes { get; set; }

    [Required]
    [ValidateObjectMembers]
    public InitialAccountOption InitialAccount { get; set; } = default!;
}

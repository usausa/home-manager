namespace HomeManager.Services;

public sealed class InitialAccountOption
{
    [Required]
    public string Id { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

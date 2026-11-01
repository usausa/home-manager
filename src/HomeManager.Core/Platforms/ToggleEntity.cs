namespace HomeManager.Platforms;

// ON / OFF できるエンティティ (HA の ToggleEntity)
public abstract class ToggleEntity : HomeEntity
{
    private protected ToggleEntity()
    {
    }

    // nullは不明
    public abstract bool? IsOn { get; }

    public sealed override string? State => IsOn switch
    {
        true => StateValues.On,
        false => StateValues.Off,
        null => null
    };

    public abstract ValueTask TurnOnAsync(CancellationToken cancel);

    public abstract ValueTask TurnOffAsync(CancellationToken cancel);

    public virtual ValueTask ToggleAsync(CancellationToken cancel) =>
        IsOn == true ? TurnOffAsync(cancel) : TurnOnAsync(cancel);
}

namespace HomeManager.Platforms;

// 2値センサー (HA の BinarySensorEntity。人感・ドアなど)
public abstract class BinarySensorEntity : HomeEntity
{
    public sealed override string Domain => Domains.BinarySensor;

    // nullは不明
    public abstract bool? IsOn { get; }

    public sealed override string? State => IsOn switch
    {
        true => StateValues.On,
        false => StateValues.Off,
        null => null
    };
}

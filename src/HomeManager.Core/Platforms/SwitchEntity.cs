namespace HomeManager.Platforms;

// スイッチ (HA の SwitchEntity。コンセントなど)
public abstract class SwitchEntity : ToggleEntity
{
    public sealed override string Domain => Domains.Switch;
}

namespace HomeManager.Platforms;

// 照明 (HA の LightEntity。P1は明るさまで、色温度・色はP2)
public abstract class LightEntity : ToggleEntity
{
    public sealed override string Domain => Domains.Light;

    // 明るさ (0〜255)。nullは不明
    public virtual int? Brightness => null;

    public virtual IReadOnlyList<string> SupportedColorModes => [ColorModes.OnOff];

    public virtual string ColorMode => SupportsBrightness ? ColorModes.Brightness : ColorModes.OnOff;

    public bool SupportsBrightness => SupportedColorModes.Contains(ColorModes.Brightness);

    public sealed override ValueTask TurnOnAsync(CancellationToken cancel) =>
        TurnOnAsync(new LightTurnOnArgs(), cancel);

    public abstract ValueTask TurnOnAsync(LightTurnOnArgs args, CancellationToken cancel);

    private protected override void CollectCapabilityAttributes(Dictionary<string, object?> attributes)
    {
        attributes[AttributeNames.SupportedColorModes] = SupportedColorModes.ToArray();
    }

    private protected override void CollectStateAttributes(Dictionary<string, object?> attributes)
    {
        // 消えているときは明るさを持たない (HAと同じくnullで書く)
        var on = IsOn == true;
        attributes[AttributeNames.ColorMode] = on ? ColorMode : null;
        if (SupportsBrightness)
        {
            attributes[AttributeNames.Brightness] = on ? Brightness : null;
        }
    }
}

// light.turn_onの入力
public sealed record LightTurnOnArgs(int? Brightness = null, double? Transition = null);

// 色の扱い方 (HA の ColorMode)
public static class ColorModes
{
    public const string OnOff = "onoff";

    public const string Brightness = "brightness";
}

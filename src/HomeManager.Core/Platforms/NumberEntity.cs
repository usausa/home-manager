namespace HomeManager.Platforms;

// 数値の設定 (HA の NumberEntity。音量・目標値など)
public abstract class NumberEntity : HomeEntity
{
    public sealed override string Domain => Domains.Number;

    // nullは不明
    public abstract double? NativeValue { get; }

    public virtual double NativeMinValue => 0;

    public virtual double NativeMaxValue => 100;

    public virtual double NativeStep => 1;

    // auto / box / slider (画面の入力部品)
    public virtual string Mode => NumberModes.Auto;

    public virtual string? NativeUnitOfMeasurement => null;

    public sealed override string? UnitOfMeasurement => NativeUnitOfMeasurement;

    public sealed override string? State => NativeValue?.ToString(CultureInfo.InvariantCulture);

    public abstract ValueTask SetNativeValueAsync(double value, CancellationToken cancel);

    private protected override void CollectCapabilityAttributes(Dictionary<string, object?> attributes)
    {
        attributes[AttributeNames.Min] = NativeMinValue;
        attributes[AttributeNames.Max] = NativeMaxValue;
        attributes[AttributeNames.Step] = NativeStep;
        attributes[AttributeNames.Mode] = Mode;
    }
}

public static class NumberModes
{
    public const string Auto = "auto";

    public const string Box = "box";

    public const string Slider = "slider";
}

namespace HomeManager.Platforms;

// センサー (HA の SensorEntity)。状態は値をそのまま文字列にする (表示の桁数は画面で決める)
public abstract class SensorEntity : HomeEntity
{
    public sealed override string Domain => Domains.Sensor;

    // 数値・文字列・日時。nullは不明
    public abstract object? NativeValue { get; }

    public virtual string? NativeUnitOfMeasurement => null;

    // measurement / total / total_increasing (長期統計の対象かどうか。P4)
    public virtual string? StateClass => null;

    public sealed override string? UnitOfMeasurement => NativeUnitOfMeasurement;

    public sealed override string? State => NativeValue switch
    {
        null => null,
        DateTimeOffset value => value.ToString("O", CultureInfo.InvariantCulture),
        IFormattable value => value.ToString(null, CultureInfo.InvariantCulture),
        var value => value.ToString()
    };

    private protected override void CollectCapabilityAttributes(Dictionary<string, object?> attributes)
    {
        if (StateClass is { } stateClass)
        {
            attributes[AttributeNames.StateClass] = stateClass;
        }
    }
}

public static class StateClasses
{
    public const string Measurement = "measurement";

    public const string Total = "total";

    public const string TotalIncreasing = "total_increasing";
}

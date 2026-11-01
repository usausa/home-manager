namespace HomeManager.Integration.Demo;

// 照明 (明るさあり・なし)
public sealed class DemoLight : LightEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly bool supportsBrightness;

    private bool isOn;

    private int brightness;

    public DemoLight(string objectId, string name, bool supportsBrightness, bool isOn, int? brightness)
    {
        this.objectId = objectId;
        this.name = name;
        this.supportsBrightness = supportsBrightness;
        this.isOn = isOn;
        this.brightness = brightness ?? 255;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override bool? IsOn => isOn;

    public override int? Brightness => supportsBrightness ? brightness : null;

    public override IReadOnlyList<string> SupportedColorModes => supportsBrightness ? [ColorModes.Brightness] : [ColorModes.OnOff];

    // 明るさの指定が無ければ前の明るさでつける
    public override ValueTask TurnOnAsync(LightTurnOnArgs args, CancellationToken cancel)
    {
        isOn = true;
        if (args.Brightness is { } value)
        {
            brightness = value;
        }

        WriteState();
        return ValueTask.CompletedTask;
    }

    public override ValueTask TurnOffAsync(CancellationToken cancel)
    {
        isOn = false;
        WriteState();
        return ValueTask.CompletedTask;
    }
}

// スイッチ (コンセントなど)
public sealed class DemoSwitch : SwitchEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly string? deviceClass;

    private bool isOn;

    public DemoSwitch(string objectId, string name, string? deviceClass, bool isOn)
    {
        this.objectId = objectId;
        this.name = name;
        this.deviceClass = deviceClass;
        this.isOn = isOn;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override string? DeviceClass => deviceClass;

    public override bool? IsOn => isOn;

    public override ValueTask TurnOnAsync(CancellationToken cancel)
    {
        isOn = true;
        WriteState();
        return ValueTask.CompletedTask;
    }

    public override ValueTask TurnOffAsync(CancellationToken cancel)
    {
        isOn = false;
        WriteState();
        return ValueTask.CompletedTask;
    }
}

// 値の動かし方 (幅・下限・上限・小数の桁数)
public sealed record DemoWalk(double Step, double Min, double Max, int Digits);

// センサー (一定の間隔で値が上下する)
public sealed class DemoSensor : SensorEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly string deviceClass;

    private readonly string unit;

    private readonly DemoWalk? walk;

    private double value;

    public DemoSensor(string objectId, string name, string deviceClass, string unit, double value, DemoWalk? walk)
    {
        this.objectId = objectId;
        this.name = name;
        this.deviceClass = deviceClass;
        this.unit = unit;
        this.value = value;
        this.walk = walk;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override string DeviceClass => deviceClass;

    public override string NativeUnitOfMeasurement => unit;

    public override string StateClass => StateClasses.Measurement;

    public override object NativeValue => value;

    // -1・0・+1 のどれかの幅だけ動かす
    public void Step()
    {
        if (walk is null)
        {
            return;
        }

        var next = value + (RandomNumberGenerator.GetInt32(-1, 2) * walk.Step);
        Update(Math.Round(Math.Clamp(next, walk.Min, walk.Max), walk.Digits));
    }

    public void Update(double newValue)
    {
        value = newValue;
        WriteState();
    }
}

// 2値センサー (人感・ドア)
public sealed class DemoBinarySensor : BinarySensorEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly string deviceClass;

    private bool isOn;

    public DemoBinarySensor(string objectId, string name, string deviceClass, bool isOn)
    {
        this.objectId = objectId;
        this.name = name;
        this.deviceClass = deviceClass;
        this.isOn = isOn;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override string DeviceClass => deviceClass;

    public override bool? IsOn => isOn;

    public void Update(bool newValue)
    {
        isOn = newValue;
        WriteState();
    }
}

// ボタン (押すとログに出す)
public sealed class DemoButton : ButtonEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly ILogger log;

    public DemoButton(string objectId, string name, ILogger log)
    {
        this.objectId = objectId;
        this.name = name;
        this.log = log;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override ValueTask PressAsync(CancellationToken cancel)
    {
        log.InfoDemoButtonPressed(EntityId);
        return ValueTask.CompletedTask;
    }
}

// 数値 (音量など)
public sealed class DemoNumber : NumberEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly double min;

    private readonly double max;

    private readonly double step;

    private readonly string? unit;

    private double current;

    public DemoNumber(string objectId, string name, double value, double min, double max, double step, string? unit)
    {
        this.objectId = objectId;
        this.name = name;
        current = value;
        this.min = min;
        this.max = max;
        this.step = step;
        this.unit = unit;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override double? NativeValue => current;

    public override double NativeMinValue => min;

    public override double NativeMaxValue => max;

    public override double NativeStep => step;

    public override string Mode => NumberModes.Slider;

    public override string? NativeUnitOfMeasurement => unit;

    public override ValueTask SetNativeValueAsync(double value, CancellationToken cancel)
    {
        current = value;
        WriteState();
        return ValueTask.CompletedTask;
    }
}

// 選択肢 (風量など)
public sealed class DemoSelect : SelectEntity
{
    private readonly string objectId;

    private readonly string name;

    private readonly string[] options;

    private string current;

    public DemoSelect(string objectId, string name, string[] options, string current)
    {
        this.objectId = objectId;
        this.name = name;
        this.options = options;
        this.current = current;
    }

    public override string UniqueId => objectId;

    public override string SuggestedObjectId => objectId;

    public override string Name => name;

    public override string CurrentOption => current;

    public override IReadOnlyList<string> Options => options;

    public override ValueTask SelectOptionAsync(string value, CancellationToken cancel)
    {
        current = value;
        WriteState();
        return ValueTask.CompletedTask;
    }
}

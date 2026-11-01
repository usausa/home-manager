namespace HomeManager.Host.Components.Entities;

using MudBlazor;

// 状態の表示 (HA の frontend の state display / state icon)。ドメインと種類 (device_class) と状態に応じた文言・アイコン・色
public static class EntityFormatter
{
    private const string SystemUser = "機器・システム";

    //--------------------------------------------------------------------------------
    // Domain
    //--------------------------------------------------------------------------------

    public static string GetDomainTitle(string domain) => domain switch
    {
        Domains.Light => "💡 照明",
        Domains.Switch => "🔌 スイッチ",
        Domains.Sensor => "🌡️ センサー",
        Domains.BinarySensor => "🚪 2値センサー",
        Domains.Button => "🔘 ボタン",
        Domains.Number => "🔢 数値",
        Domains.Select => "📋 選択",
        _ => domain
    };

    // 概要に並べる順番 (操作するもの → 見るもの)
    public static int GetDomainOrder(string domain) => domain switch
    {
        Domains.Light => 0,
        Domains.Switch => 1,
        Domains.Button => 2,
        Domains.Number => 3,
        Domains.Select => 4,
        Domains.Sensor => 5,
        Domains.BinarySensor => 6,
        _ => 99
    };

    //--------------------------------------------------------------------------------
    // State
    //--------------------------------------------------------------------------------

    public static bool IsActive(EntityState state) =>
        state.State == StateValues.On;

    public static bool IsAvailable(EntityState state) =>
        state.State != StateValues.Unavailable;

    public static string FormatState(EntityState state) => state.State switch
    {
        StateValues.Unavailable => "利用不可",
        StateValues.Unknown => "不明",
        _ => state.Domain switch
        {
            Domains.Light => FormatLight(state),
            Domains.Switch => FormatOnOff(state.State),
            Domains.BinarySensor => FormatBinarySensor(state.State, GetDeviceClass(state)),
            Domains.Sensor or Domains.Number => FormatValue(state.State, state.GetAttribute(AttributeNames.UnitOfMeasurement) as string),
            Domains.Button => DateTimeOffset.TryParse(state.State, CultureInfo.InvariantCulture, out var pressed) ? $"{FormatTime(pressed)} に押した" : state.State,
            _ => state.State
        }
    };

    // 明るさ (0〜255) を % にする
    public static int? GetBrightnessPercent(EntityState state) =>
        GetNumber(state.GetAttribute(AttributeNames.Brightness)) is { } brightness ? (int)Math.Round((brightness * 100) / 255) : null;

    public static double? GetNumber(object? value) => value switch
    {
        null => null,
        IConvertible convertible and not string => convertible.ToDouble(CultureInfo.InvariantCulture),
        string text when Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
        _ => null
    };

    private static string FormatLight(EntityState state) =>
        (state.State == StateValues.On) && (GetBrightnessPercent(state) is { } percent)
            ? String.Create(CultureInfo.InvariantCulture, $"オン・{percent}%")
            : FormatOnOff(state.State);

    private static string FormatOnOff(string value) => value switch
    {
        StateValues.On => "オン",
        StateValues.Off => "オフ",
        _ => value
    };

    private static string FormatBinarySensor(string value, string? deviceClass) => (deviceClass, value) switch
    {
        (DeviceClasses.Motion or DeviceClasses.Occupancy, StateValues.On) => "検知",
        (DeviceClasses.Motion or DeviceClasses.Occupancy, StateValues.Off) => "なし",
        (DeviceClasses.Door or DeviceClasses.Window, StateValues.On) => "開",
        (DeviceClasses.Door or DeviceClasses.Window, StateValues.Off) => "閉",
        _ => FormatOnOff(value)
    };

    private static string FormatValue(string value, string? unit) => unit switch
    {
        null or "" => value,
        "%" => value + unit,
        _ => $"{value} {unit}"
    };

    //--------------------------------------------------------------------------------
    // Icon
    //--------------------------------------------------------------------------------

    public static string GetIcon(EntityState state)
    {
        var active = IsActive(state);
        return state.Domain switch
        {
            Domains.Light => active ? Icons.Material.Filled.Lightbulb : Icons.Material.Outlined.Lightbulb,
            Domains.Switch => GetDeviceClass(state) == DeviceClasses.Outlet
                ? (active ? Icons.Material.Filled.ElectricalServices : Icons.Material.Filled.PowerOff)
                : (active ? Icons.Material.Filled.ToggleOn : Icons.Material.Filled.ToggleOff),
            Domains.Sensor => GetDeviceClass(state) switch
            {
                DeviceClasses.Temperature => Icons.Material.Filled.DeviceThermostat,
                DeviceClasses.Humidity => Icons.Material.Filled.WaterDrop,
                DeviceClasses.CarbonDioxide => Icons.Material.Filled.Co2,
                DeviceClasses.Power => Icons.Material.Filled.ElectricBolt,
                DeviceClasses.Energy => Icons.Material.Filled.ElectricMeter,
                DeviceClasses.Illuminance => Icons.Material.Filled.LightMode,
                _ => Icons.Material.Filled.Sensors
            },
            Domains.BinarySensor => GetDeviceClass(state) switch
            {
                DeviceClasses.Motion or DeviceClasses.Occupancy => active ? Icons.Material.Filled.DirectionsRun : Icons.Material.Filled.DirectionsWalk,
                DeviceClasses.Door => active ? Icons.Material.Filled.MeetingRoom : Icons.Material.Filled.DoorFront,
                DeviceClasses.Window => Icons.Material.Filled.Window,
                _ => active ? Icons.Material.Filled.RadioButtonChecked : Icons.Material.Filled.RadioButtonUnchecked
            },
            Domains.Button => Icons.Material.Filled.TouchApp,
            Domains.Number => Icons.Material.Filled.Tune,
            Domains.Select => Icons.Material.Filled.FormatListBulleted,
            _ => Icons.Material.Filled.QuestionMark
        };
    }

    public static Color GetColor(EntityState state)
    {
        if (!IsAvailable(state))
        {
            return Color.Default;
        }

        return state.Domain switch
        {
            Domains.Light => IsActive(state) ? Color.Warning : Color.Default,
            Domains.Switch or Domains.BinarySensor => IsActive(state) ? Color.Primary : Color.Default,
            _ => Color.Info
        };
    }

    //--------------------------------------------------------------------------------
    // Time / Context
    //--------------------------------------------------------------------------------

    public static string FormatTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

    public static string FormatRelative(DateTimeOffset value, DateTimeOffset now)
    {
        var elapsed = now - value;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "たった今";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return String.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalMinutes} 分前");
        }

        return elapsed < TimeSpan.FromDays(1)
            ? String.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalHours} 時間前")
            : String.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalDays} 日前");
    }

    // 状態を変えた操作者 (Contextのユーザー)。ユーザーが無ければ機器かシステムの変化
    public static string FormatUser(EventContext context) =>
        context.UserId ?? SystemUser;

    private static string? GetDeviceClass(EntityState state) =>
        state.GetAttribute(AttributeNames.DeviceClass) as string;
}

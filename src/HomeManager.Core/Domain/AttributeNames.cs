namespace HomeManager.Domain;

// 属性の名前 (HA と同じ名前にする)
public static class AttributeNames
{
    // 共通

    public const string FriendlyName = "friendly_name";

    public const string UnitOfMeasurement = "unit_of_measurement";

    public const string DeviceClass = "device_class";

    public const string AssumedState = "assumed_state";

    // sensor

    public const string StateClass = "state_class";

    // light

    public const string Brightness = "brightness";

    public const string ColorMode = "color_mode";

    public const string SupportedColorModes = "supported_color_modes";

    // number

    public const string Min = "min";

    public const string Max = "max";

    public const string Step = "step";

    public const string Mode = "mode";

    // select

    public const string Options = "options";
}

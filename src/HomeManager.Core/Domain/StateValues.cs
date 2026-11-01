namespace HomeManager.Domain;

// 状態の共通の値 (HA の STATE_ON など)
public static class StateValues
{
    public const int MaxLength = 255;

    public const string On = "on";

    public const string Off = "off";

    public const string Unknown = "unknown";

    public const string Unavailable = "unavailable";
}

namespace HomeManager.Domain;

// 機器の種類 (HA の device_class。表示のアイコンや文言を変えるのに使う)
public static class DeviceClasses
{
    // sensor

    public const string Temperature = "temperature";

    public const string Humidity = "humidity";

    public const string CarbonDioxide = "carbon_dioxide";

    public const string Power = "power";

    public const string Energy = "energy";

    public const string Illuminance = "illuminance";

    // binary_sensor

    public const string Motion = "motion";

    public const string Occupancy = "occupancy";

    public const string Door = "door";

    public const string Window = "window";

    // switch

    public const string Outlet = "outlet";
}

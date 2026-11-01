namespace HomeManager.Domain;

// ドメイン (HA の homeassistant と各エンティティの種類)
public static class Domains
{
    public const string HomeManager = "homemanager";

    public const string Light = "light";

    public const string Switch = "switch";

    public const string Sensor = "sensor";

    public const string BinarySensor = "binary_sensor";

    public const string Button = "button";

    public const string Number = "number";

    public const string Select = "select";
}

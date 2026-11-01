namespace HomeManager.Integrations;

// インテグレーションのメタ情報 (HA の manifest.json)
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationAttribute : Attribute
{
    public IntegrationAttribute(string domain)
    {
        Domain = domain;
    }

    public string Domain { get; }

    public string Name { get; set; } = string.Empty;

    public IotClass IotClass { get; set; }
}

// 機器とのやり取りの仕方 (HA の iot_class)
public enum IotClass
{
    Calculated,
    LocalPush,
    LocalPolling,
    CloudPush,
    CloudPolling,
    AssumedState
}

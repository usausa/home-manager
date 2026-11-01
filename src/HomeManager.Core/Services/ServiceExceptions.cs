namespace HomeManager.Services;

// 未登録のサービス
public sealed class ServiceNotFoundException : Exception
{
    public ServiceNotFoundException(string domain, string service)
        : base($"サービス {domain}.{service} は登録されていません。")
    {
        Domain = domain;
        Service = service;
    }

    public string Domain { get; }

    public string Service { get; }
}

// サービスの入力の誤り (HA の ServiceValidationError。メッセージはそのまま画面に出す)
public sealed class ServiceValidationException : Exception
{
    public ServiceValidationException(string message)
        : base(message)
    {
    }
}

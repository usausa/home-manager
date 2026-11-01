namespace HomeManager.Services;

using System.Text.Json.Nodes;

// サービスの登録と呼び出し (HA の ServiceRegistry。domain.serviceで機器を操作する)
public interface IServiceRegistry
{
    void Register(ServiceDefinition definition, Func<ServiceCall, CancellationToken, ValueTask<object?>> handler);

    bool Remove(string domain, string service);

    bool Contains(string domain, string service);

    ServiceDefinition? FindDefinition(string domain, string service);

    IReadOnlyList<ServiceDefinition> ListServices();

    // 呼ぶ前にcall_serviceを発行する。入力の誤りはServiceValidationException、未登録はServiceNotFoundException
    ValueTask<object?> CallAsync(
        string domain,
        string service,
        JsonObject? data,
        ServiceTarget? target,
        EventContext context,
        CancellationToken cancel = default);
}

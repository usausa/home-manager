namespace HomeManager.Services;

public interface ISystemInfoService
{
    ValueTask<SystemInfo> GetInfoAsync(CancellationToken cancellationToken = default);
}

namespace HomeManager.Services;

using System.Reflection;
using System.Runtime.InteropServices;

using HomeManager.Accessors;

// システム情報 (SCR-362)。版・稼働時間・DB・インテグレーションの状況をまとめる
public sealed class SystemInfoService : ISystemInfoService
{
    private readonly ILogger<SystemInfoService> log;

    private readonly SystemAccessor systemAccessor;

    private readonly IStateMachine stateMachine;

    private readonly IServiceRegistry serviceRegistry;

    private readonly IntegrationManager integrationManager;

    public SystemInfoService(
        ILogger<SystemInfoService> log,
        SystemAccessor systemAccessor,
        IStateMachine stateMachine,
        IServiceRegistry serviceRegistry,
        IntegrationManager integrationManager,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.systemAccessor = systemAccessor;
        this.stateMachine = stateMachine;
        this.serviceRegistry = serviceRegistry;
        this.integrationManager = integrationManager;
        // 稼働時間の起点 (起動時に作る)
        StartedAt = timeProvider.GetUtcNow();
    }

    public DateTimeOffset StartedAt { get; }

    public async ValueTask<SystemInfo> GetInfoAsync(CancellationToken cancellationToken = default) =>
        new(
            GetVersion(),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            StartedAt,
            await QueryDatabaseAsync(cancellationToken),
            stateMachine.Count,
            serviceRegistry.ListServices().Count,
            integrationManager.ListIntegrations());

    // DBを読めなくても画面は出す
    private async ValueTask<DatabaseInfo?> QueryDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var version = await systemAccessor.QueryVersionAsync(cancellationToken);
            var file = await systemAccessor.QueryFileAsync(cancellationToken);
            var size = await systemAccessor.QueryDatabaseSizeAsync(cancellationToken);
            return new DatabaseInfo(version, file, size);
        }
        catch (DbException ex)
        {
            log.WarnDatabaseInfoFailed(ex);
            return null;
        }
    }

    // 版とコミット (0.0.1+4b56081… の形)
    private static string GetVersion()
    {
        var assembly = typeof(SystemInfoService).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
               assembly.GetName().Version?.ToString() ??
               string.Empty;
    }
}

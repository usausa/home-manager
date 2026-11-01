namespace HomeManager.Host.Components.Entities;

using System.Text.Json.Nodes;

using MudBlazor;

// 画面からの機器の操作 (タイルのアイコン・詳細ダイアログ)。操作者をContextに付けてサービスを呼ぶ
// 画面のイベント (AppPageBaseのスコープの中) から呼ぶ
public sealed class EntityOperations
{
    private readonly IServiceRegistry serviceRegistry;

    private readonly ServiceContextProvider contextProvider;

    private readonly ISnackbar snackbar;

    public EntityOperations(
        IServiceRegistry serviceRegistry,
        ServiceContextProvider contextProvider,
        ISnackbar snackbar)
    {
        this.serviceRegistry = serviceRegistry;
        this.contextProvider = contextProvider;
        this.snackbar = snackbar;
    }

    // アイコンを押したときのサービス (nullは操作が無く、詳細を開く)
    public static string? GetDefaultService(string domain) => domain switch
    {
        _ when PlatformServices.IsToggleable(domain) => PlatformServices.Toggle,
        Domains.Button => PlatformServices.Press,
        _ => null
    };

    public static string GetActionLabel(string domain) => domain switch
    {
        _ when PlatformServices.IsToggleable(domain) => "切り替える",
        Domains.Button => "押す",
        _ => "詳細"
    };

    // アイコンを押したときの操作。操作が無いドメインはfalseを返す
    public async ValueTask<bool> ActAsync(EntityState state)
    {
        var service = GetDefaultService(state.Domain);
        if (service is null)
        {
            return false;
        }

        await CallAsync(state.Domain, service, state.EntityId);
        return true;
    }

    // 入力の誤りと未登録のサービスは画面に出す (それ以外の例外はErrorBoundaryへ)
    public async ValueTask<bool> CallAsync(string domain, string service, string entityId, JsonObject? data = null)
    {
        try
        {
            await serviceRegistry.CallAsync(domain, service, data, ServiceTarget.Of(entityId), contextProvider.CreateEventContext());
            return true;
        }
        catch (Exception ex) when (ex is ServiceValidationException or ServiceNotFoundException)
        {
            snackbar.AddError(ex.Message);
            return false;
        }
    }
}

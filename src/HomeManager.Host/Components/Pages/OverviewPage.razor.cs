namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Dialogs;
using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 概要 (SCR-101)。設定を保存しない自動生成のダッシュボード。P1はドメインごと、P2からエリアごとに並べる
// どこかで状態が変わると、開いている全員の画面のタイルが変わる
public sealed partial class OverviewPage
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

    private List<EntityGroup> groups = [];

    private string? search;

    private StateSubscription? subscription;

    private IDisposable? configSubscription;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required IStateMachine StateMachine { get; set; }

    [Inject]
    public required IEventBus EventBus { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required ICoreConfigService CoreConfig { get; set; }

    [Inject]
    public required EntityOperations Operations { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery(Name = "q")]
    public string? Query { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        search = Query;
        Reload();

        // 状態が変わったら間引いて描き直す
        subscription = new StateSubscription(
            EventBus,
            TimeProvider,
            RefreshInterval,
            null,
            () => InvokeAsync(() =>
            {
                Reload();
                StateHasChanged();
            }));

        // 全体設定が変わったら描き直す (見出しの家の名前・時刻のタイムゾーン)
        configSubscription = EventBus.Subscribe<CoreConfigUpdatedEvent>((_, _) => new ValueTask(InvokeAsync(StateHasChanged)));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            subscription?.Dispose();
            configSubscription?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Reload()
    {
        groups =
        [
            .. StateMachine.GetStates()
                .Where(Match)
                .GroupBy(static x => x.Domain, StringComparer.Ordinal)
                .OrderBy(static x => EntityFormatter.GetDomainOrder(x.Key))
                .ThenBy(static x => x.Key, StringComparer.Ordinal)
                .Select(static x => new EntityGroup(
                    EntityFormatter.GetDomainTitle(x.Key),
                    [.. x.OrderBy(static y => y.Name, StringComparer.CurrentCulture).ThenBy(static y => y.EntityId, StringComparer.Ordinal)]))
        ];
    }

    private bool Match(EntityState state) =>
        String.IsNullOrWhiteSpace(search) ||
        state.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        state.EntityId.Contains(search, StringComparison.OrdinalIgnoreCase);

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnSearchChanged(string? value)
    {
        search = value;
        Reload();

        // 絞り込みをURLに載せる (共有・再読み込みで戻せるように)
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("q", String.IsNullOrWhiteSpace(value) ? null : value), replace: true);
    }

    private async Task OnTileAction(EntityState state)
    {
        if (!await Operations.ActAsync(state))
        {
            await DialogService.ShowEntityDetail(state.EntityId);
            return;
        }

        Reload();
    }

    private async Task OnTileOpen(EntityState state)
    {
        await DialogService.ShowEntityDetail(state.EntityId);
    }

    //--------------------------------------------------------------------------------
    // Model
    //--------------------------------------------------------------------------------

    private sealed record EntityGroup(string Title, IReadOnlyList<EntityState> States);
}

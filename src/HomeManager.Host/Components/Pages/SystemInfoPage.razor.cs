namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Application.Circuits;
using HomeManager.Host.Components.Entities;
using HomeManager.Host.Components.Settings;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// システム情報 (SCR-362)。版・稼働時間・DB・インテグレーション・接続中の回線。回線の出入りと稼働時間はその場で変わる
public sealed partial class SystemInfoPage
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private SystemInfo? info;

    private IReadOnlyList<CircuitInfo> circuits = [];

    private ITimer? timer;

    private bool busy;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required ISystemInfoService SystemInfoService { get; set; }

    [Inject]
    public required ICoreConfigService CoreConfig { get; set; }

    [Inject]
    public required CircuitTracker CircuitTracker { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    private bool Loaded => info is not null;

    private SystemInfo Info => info!;

    private bool HasDatabase => info?.Database is not null;

    private string VersionText => SystemFormatter.FormatVersion(Info.Version);

    private string StartedText => FormatTime(Info.StartedAt);

    private string UptimeText => SystemFormatter.FormatDuration(TimeProvider.GetUtcNow() - Info.StartedAt);

    private string DatabaseSizeText => SystemFormatter.FormatBytes(Info.Database?.Size ?? 0);

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        circuits = CircuitTracker.List();
        CircuitTracker.Changed += OnCircuitChanged;
        // 稼働時間の表示を進める
        timer = TimeProvider.CreateTimer(_ => OnTick(), null, TickInterval, TickInterval);

        info = await SystemInfoService.GetInfoAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CircuitTracker.Changed -= OnCircuitChanged;
            timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private async Task OnRefreshAsync()
    {
        busy = true;
        try
        {
            info = await SystemInfoService.GetInfoAsync();
            circuits = CircuitTracker.List();
        }
        finally
        {
            busy = false;
        }
    }

    private void OnTick() => _ = InvokeAsync(StateHasChanged);

    private void OnCircuitChanged(object? sender, EventArgs e) =>
        _ = InvokeAsync(() =>
        {
            circuits = CircuitTracker.List();
            StateHasChanged();
        });

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private string FormatTime(DateTimeOffset? value) =>
        value is null ? "—" : EntityFormatter.FormatTime(value.Value, CoreConfig.TimeZone);

    private static string GetStatusText(IntegrationStatus status) => status switch
    {
        IntegrationStatus.Loaded => "読み込み済み",
        IntegrationStatus.SetupError => "エラー",
        _ => "未読み込み"
    };

    private static Color GetStatusColor(IntegrationStatus status) => status switch
    {
        IntegrationStatus.Loaded => Color.Success,
        IntegrationStatus.SetupError => Color.Error,
        _ => Color.Default
    };
}

namespace HomeManager.Host.Components.Dialogs;

using System.Text.Json.Nodes;

using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// エンティティ詳細 (DLG-01)。操作と属性のタブ、最後に変えた時刻と操作者。開いている間は状態の変化を反映する
public sealed partial class EntityDetailDialog
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

    private EntityState? state;

    private StateSubscription? subscription;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required string EntityId { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

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
    public required EntityOperations Operations { get; set; }

    private bool Found => state is not null;

    private string Domain => state?.Domain ?? string.Empty;

    private string Title => state?.Name ?? EntityId;

    private string Icon => state is null ? Icons.Material.Filled.QuestionMark : EntityFormatter.GetIcon(state);

    private Color IconColor => state is null ? Color.Default : EntityFormatter.GetColor(state);

    private string StateText => state is null ? string.Empty : EntityFormatter.FormatState(state);

    private bool Available => (state is not null) && EntityFormatter.IsAvailable(state);

    private bool IsToggle => PlatformServices.IsToggleable(Domain);

    private bool IsOn => state?.State == StateValues.On;

    private bool ShowBrightness =>
        (Domain == Domains.Light) &&
        (state?.GetAttribute(AttributeNames.SupportedColorModes) is IEnumerable<string> modes) &&
        modes.Contains(ColorModes.Brightness);

    private int BrightnessPercent => (state is null ? null : EntityFormatter.GetBrightnessPercent(state)) ?? 100;

    // 消えているときは明るさを持たない (スライダーを動かすと、その明るさでつく)
    private string BrightnessText => IsOn ? String.Create(CultureInfo.InvariantCulture, $"{BrightnessPercent}%") : "消灯中（動かすとつきます）";

    private bool IsButton => Domain == Domains.Button;

    private bool IsNumber => Domain == Domains.Number;

    private double NumberValue => EntityFormatter.GetNumber(state?.State) ?? NumberMin;

    private double NumberMin => EntityFormatter.GetNumber(state?.GetAttribute(AttributeNames.Min)) ?? 0;

    private double NumberMax => EntityFormatter.GetNumber(state?.GetAttribute(AttributeNames.Max)) ?? 100;

    private double NumberStep => EntityFormatter.GetNumber(state?.GetAttribute(AttributeNames.Step)) ?? 1;

    private string NumberMinText => JsonFormatter.FormatValue(NumberMin);

    private string NumberMaxText => JsonFormatter.FormatValue(NumberMax);

    private bool IsSelect => Domain == Domains.Select;

    private IReadOnlyList<string> Options => state?.GetAttribute(AttributeNames.Options) as IReadOnlyList<string> ?? [];

    private string? CurrentOption => IsSelect && Available && (state?.State != StateValues.Unknown) ? state?.State : null;

    private IReadOnlyList<KeyValuePair<string, object?>> AttributeList =>
        state is null ? [] : [.. state.Attributes.OrderBy(static x => x.Key, StringComparer.Ordinal)];

    private string LastChangedText => state is null ? string.Empty : FormatTime(state.LastChanged);

    private string LastUpdatedText => state is null ? string.Empty : FormatTime(state.LastUpdated);

    private string UserText => state is null ? string.Empty : EntityFormatter.FormatUser(state.Context);

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        Reload();
        subscription = new StateSubscription(
            EventBus,
            TimeProvider,
            RefreshInterval,
            x => x == EntityId,
            () => InvokeAsync(() =>
            {
                Reload();
                StateHasChanged();
            }));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            subscription?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Reload() => state = StateMachine.GetState(EntityId);

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private async Task OnSwitchChanged(bool value)
    {
        await Operations.CallAsync(Domain, value ? PlatformServices.TurnOn : PlatformServices.TurnOff, EntityId);
        Reload();
    }

    private async Task OnBrightnessChanged(int value)
    {
        await Operations.CallAsync(Domains.Light, PlatformServices.TurnOn, EntityId, new JsonObject { ["brightness_pct"] = value });
        Reload();
    }

    private async Task OnPressClick()
    {
        await Operations.CallAsync(Domains.Button, PlatformServices.Press, EntityId);
        Reload();
    }

    private async Task OnNumberChanged(double value)
    {
        await Operations.CallAsync(Domains.Number, PlatformServices.SetValue, EntityId, new JsonObject { ["value"] = value });
        Reload();
    }

    private async Task OnOptionChanged(string? value)
    {
        if (value is null)
        {
            return;
        }

        await Operations.CallAsync(Domains.Select, PlatformServices.SelectOption, EntityId, new JsonObject { ["option"] = value });
        Reload();
    }

    private void OnCloseClick() => MudDialog.Close();

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private string FormatTime(DateTimeOffset value) =>
        $"{EntityFormatter.FormatTime(value)}（{EntityFormatter.FormatRelative(value, TimeProvider.GetUtcNow())}）";
}

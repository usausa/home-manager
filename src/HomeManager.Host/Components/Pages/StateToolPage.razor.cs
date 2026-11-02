namespace HomeManager.Host.Components.Pages;

using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 開発者ツールの状態 (SCR-201)。全エンティティの状態と属性の一覧と、デバッグ用の状態の上書き
public sealed partial class StateToolPage
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(500);

    private List<EntityState> states = [];

    private string? filter;

    private string? editEntityId;

    private string? editState;

    private string? editAttributes;

    private StateSubscription? subscription;

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
    public required ServiceContextProvider ContextProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery(Name = "q")]
    public string? Query { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        filter = Query;
        Reload();

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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            subscription?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Reload()
    {
        states = [.. StateMachine.GetStates().Where(Match).OrderBy(static x => x.EntityId, StringComparer.Ordinal)];
    }

    private bool Match(EntityState state) =>
        String.IsNullOrWhiteSpace(filter) ||
        state.EntityId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        state.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        state.State.Contains(filter, StringComparison.OrdinalIgnoreCase);

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnFilterChanged(string? value)
    {
        filter = value;
        Reload();
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("q", String.IsNullOrWhiteSpace(value) ? null : value), replace: true);
    }

    private void OnEditClick(EntityState state)
    {
        editEntityId = state.EntityId;
        editState = state.State;
        editAttributes = JsonFormatter.FormatObject(JsonFormatter.ToJsonObject(state.Attributes));
    }

    private void OnSetClick()
    {
        var entityId = editEntityId?.Trim() ?? string.Empty;
        if (!EntityIds.IsValid(entityId))
        {
            Snackbar.AddError("entity_id は「ドメイン.名前」（英小文字・数字・_）で入力してください。");
            return;
        }

        var state = editState?.Trim() ?? string.Empty;
        if (state.Length > StateValues.MaxLength)
        {
            Snackbar.AddError(String.Create(CultureInfo.InvariantCulture, $"状態は {StateValues.MaxLength} 文字以内で入力してください。"));
            return;
        }

        if (!JsonFormatter.TryParseObject(editAttributes, out var json, out var error))
        {
            Snackbar.AddError(error);
            return;
        }

        var attributes = json is null ? null : AttributeJson.ToAttributes(json);
        StateMachine.SetState(entityId, state, attributes, ContextProvider.CreateEventContext());
        Snackbar.AddSuccess($"{entityId} の状態を設定しました。");
        Reload();
    }
}

namespace HomeManager.Host.Components.Cards;

using HomeManager.Host.Components.Entities;

using Microsoft.AspNetCore.Components;

// タイル (CARD-01)。アイコンを押すとON / OFF (ボタンは押す)、それ以外の場所を押すと詳細を開く (HA の tile カードと同じ)
public sealed partial class EntityTile
{
    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    [EditorRequired]
    public required EntityState State { get; set; }

    // アイコンを押した (ON / OFF・押す)
    [Parameter]
    public EventCallback<EntityState> OnAction { get; set; }

    // 詳細を開く
    [Parameter]
    public EventCallback<EntityState> OnOpen { get; set; }

    [Inject]
    public required ICoreConfigService CoreConfig { get; set; }

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    private string TileClass => EntityFormatter.IsActive(State) ? "entity-tile entity-tile-active" : "entity-tile";

    private string ActionLabel => EntityOperations.GetActionLabel(State.Domain);

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private Task OnIconClick() => OnAction.InvokeAsync(State);

    private Task OnTileClick() => OnOpen.InvokeAsync(State);
}

namespace HomeManager.Services;

// ステートマシン (HA の StateMachine)。entity_idごとの最新の状態を持ち、変化をstate_changedとして発行する
public interface IStateMachine
{
    int Count { get; }

    EntityState? GetState(string entityId);

    IReadOnlyList<EntityState> GetStates(string? domain = null);

    // 状態と属性が同じならstate_reported、違えばstate_changedを発行する。forceUpdateは同じでもstate_changedにする
    EntityState SetState(
        string entityId,
        string state,
        IReadOnlyDictionary<string, object?>? attributes,
        EventContext context,
        bool forceUpdate = false);

    bool RemoveState(string entityId, EventContext context);
}

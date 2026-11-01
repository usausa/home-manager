namespace HomeManager.Domain;

// 因果関係の追跡 (HA の Context)。誰が・何が状態を変えたかを状態とイベントに残す
public sealed record EventContext(string Id, string? UserId = null, string? ParentId = null)
{
    // IDは時刻順に並ぶGUID v7にする
    public static EventContext Create(string? userId = null, string? parentId = null) =>
        new(Guid.CreateVersion7().ToString("D", CultureInfo.InvariantCulture), userId, parentId);

    // この操作を起点にした子の操作 (オートメーションから呼んだサービスなど)
    public EventContext CreateChild() => Create(UserId, Id);
}

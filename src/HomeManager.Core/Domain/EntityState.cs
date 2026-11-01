namespace HomeManager.Domain;

using System.Text.Json.Serialization;

// 状態のスナップショット (イミュータブル。HA の State)
public sealed record EntityState(
    string EntityId,
    string State,
    IReadOnlyDictionary<string, object?> Attributes,
    DateTimeOffset LastChanged,
    DateTimeOffset LastUpdated,
    DateTimeOffset LastReported,
    EventContext Context)
{
    [JsonIgnore]
    public string Domain => EntityIds.GetDomain(EntityId);

    [JsonIgnore]
    public string ObjectId => EntityIds.GetObjectId(EntityId);

    // 表示名 (friendly_name。無ければobject_id)
    [JsonIgnore]
    public string Name => Attributes.GetValueOrDefault(AttributeNames.FriendlyName) is string { Length: > 0 } name ? name : ObjectId;

    public object? GetAttribute(string name) => Attributes.GetValueOrDefault(name);
}

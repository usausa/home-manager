namespace HomeManager.Domain;

using System.Text.Json;
using System.Text.Json.Nodes;

// JSONの属性を状態の属性 (数値・文字列・真偽値・配列・入れ子の辞書) に変える (開発者ツール・API・オートメーションの入力)
public static class AttributeJson
{
    public static Dictionary<string, object?> ToAttributes(JsonObject json)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, node) in json)
        {
            attributes[key] = ToValue(node);
        }

        return attributes;
    }

    public static object? ToValue(JsonNode? node) => node switch
    {
        null => null,
        JsonObject json => ToAttributes(json),
        JsonArray array => array.Select(ToValue).ToArray(),
        JsonValue value => value.GetValueKind() switch
        {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetValue<long>(out var integer) => integer,
            JsonValueKind.Number when value.TryGetValue<double>(out var number) => number,
            _ => null
        },
        _ => null
    };
}

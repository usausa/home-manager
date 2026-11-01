namespace HomeManager.Host.Components.Entities;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

// 開発者ツール・詳細ダイアログのJSONの表示と入力
public static class JsonFormatter
{
    // HA の開発者ツールと同じくsnake_caseで出す
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string Format(object? value) =>
        JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), IndentedOptions);

    // 属性の値 (文字列と数値はそのまま、配列と辞書は1行のJSON)
    public static string FormatValue(object? value) => value switch
    {
        null => "null",
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => JsonSerializer.Serialize(value, value.GetType(), CompactOptions)
    };

    // 属性を「名前: 値」の行にする (HA の開発者ツールと同じ見せ方)
    public static string FormatAttributes(IReadOnlyDictionary<string, object?> attributes) =>
        String.Join('\n', attributes.OrderBy(static x => x.Key, StringComparer.Ordinal).Select(static x => $"{x.Key}: {FormatValue(x.Value)}"));

    public static string FormatObject(JsonObject value) =>
        value.ToJsonString(IndentedOptions);

    public static JsonObject ToJsonObject(IReadOnlyDictionary<string, object?> attributes)
    {
        var json = new JsonObject();
        foreach (var (key, value) in attributes.OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            json[key] = JsonSerializer.SerializeToNode(value, value?.GetType() ?? typeof(object), CompactOptions);
        }

        return json;
    }

    // 空はnull。JSONのオブジェクト以外は誤り
    public static bool TryParseObject(string? text, out JsonObject? value, [NotNullWhen(false)] out string? error)
    {
        value = null;
        error = null;
        if (String.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        try
        {
            if (JsonNode.Parse(text) is JsonObject json)
            {
                value = json;
                return true;
            }

            error = "JSON のオブジェクト（{ ... }）で入力してください。";
            return false;
        }
        catch (JsonException ex)
        {
            error = $"JSON の形式が正しくありません。{ex.Message}";
            return false;
        }
    }

    // 例の文字列をJSONの値にする (数値・真偽値はその型、それ以外は文字列)
    public static JsonNode? ParseExample(string example)
    {
        if (Double.TryParse(example, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        return Boolean.TryParse(example, out var flag) ? flag : example;
    }
}

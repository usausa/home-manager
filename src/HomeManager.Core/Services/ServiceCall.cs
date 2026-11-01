namespace HomeManager.Services;

using System.Text.Json;
using System.Text.Json.Nodes;

// サービスの呼び出し (HA の ServiceCall)。入力は画面・API・オートメーションのどこから来ても同じJSONで受ける
public sealed record ServiceCall(
    string Domain,
    string Service,
    JsonObject Data,
    ServiceTarget Target,
    EventContext Context)
{
    // 数値 (文字列の数値も受け付ける)
    public double? GetNumber(string name)
    {
        var node = Data[name];
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            var kind = value.GetValueKind();
            if ((kind == JsonValueKind.Number) &&
                Double.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            if ((kind == JsonValueKind.String) &&
                Double.TryParse(value.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        throw new ServiceValidationException($"{name} は数値で指定してください。");
    }

    public string? GetText(string name)
    {
        var node = Data[name];
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            switch (value.GetValueKind())
            {
                case JsonValueKind.String:
                    return value.GetValue<string>();
                case JsonValueKind.Number:
                    return value.ToJsonString();
            }
        }

        throw new ServiceValidationException($"{name} は文字列で指定してください。");
    }

    public bool? GetFlag(string name)
    {
        var node = Data[name];
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            switch (value.GetValueKind())
            {
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String when Boolean.TryParse(value.GetValue<string>(), out var flag):
                    return flag;
            }
        }

        throw new ServiceValidationException($"{name} は true か false で指定してください。");
    }
}

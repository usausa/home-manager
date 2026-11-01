namespace HomeManager.Domain;

using System.Text.RegularExpressions;

// entity_id (domain.object_id) の検証・分解・生成
public static partial class EntityIds
{
    public const int MaxLength = 255;

    public static bool IsValid(string entityId) =>
        (entityId.Length <= MaxLength) && EntityIdPattern().IsMatch(entityId);

    public static bool IsValidSlug(string value) =>
        SlugPattern().IsMatch(value);

    public static string GetDomain(string entityId)
    {
        var index = entityId.IndexOf('.', StringComparison.Ordinal);
        return index < 0 ? string.Empty : entityId[..index];
    }

    public static string GetObjectId(string entityId)
    {
        var index = entityId.IndexOf('.', StringComparison.Ordinal);
        return index < 0 ? entityId : entityId[(index + 1)..];
    }

    public static bool InDomain(string entityId, string domain) =>
        (entityId.Length > domain.Length) && (entityId[domain.Length] == '.') && entityId.StartsWith(domain, StringComparison.Ordinal);

    // 名前からobject_idの候補を作る。英数字以外は_にまとめるため、日本語だけの名前は空になる
    public static string Slugify(string? value)
    {
        if (String.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var separator = false;
        foreach (var c in value)
        {
            var lower = c is >= 'A' and <= 'Z' ? (char)(c + ('a' - 'A')) : c;
            if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (separator && (builder.Length > 0))
                {
                    builder.Append('_');
                }

                separator = false;
                builder.Append(lower);
            }
            else
            {
                separator = true;
            }
        }

        return builder.ToString();
    }

    // HAと同じ規則 (英小文字・数字・_。先頭と末尾の_と連続する_は不可)
    [GeneratedRegex(@"^(?!.+__)(?!_)[\da-z_]+(?<!_)\.(?!_)[\da-z_]+(?<!_)$")]
    private static partial Regex EntityIdPattern();

    [GeneratedRegex(@"^(?!.*__)(?!_)[\da-z_]+(?<!_)$")]
    private static partial Regex SlugPattern();
}

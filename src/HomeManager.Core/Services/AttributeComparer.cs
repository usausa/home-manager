namespace HomeManager.Services;

using System.Text.Json.Nodes;

// 属性の比較 (HA の dict の == と同じく中身で比べる。値は数値・文字列・真偽値・配列・入れ子の辞書・JSON)
public static class AttributeComparer
{
    public static bool AreEqual(IReadOnlyDictionary<string, object?> x, IReadOnlyDictionary<string, object?> y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (var (key, value) in x)
        {
            if (!y.TryGetValue(key, out var other) || !ValueEquals(value, other))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValueEquals(object? x, object? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if ((x is null) || (y is null))
        {
            return false;
        }

        return (x, y) switch
        {
            (string sx, string sy) => String.Equals(sx, sy, StringComparison.Ordinal),
            (JsonNode nx, JsonNode ny) => JsonNode.DeepEquals(nx, ny),
            (IReadOnlyDictionary<string, object?> dx, IReadOnlyDictionary<string, object?> dy) => AreEqual(dx, dy),
            (string, _) or (_, string) => false,
            (IEnumerable ex, IEnumerable ey) => SequenceEquals(ex, ey),
            _ => x.Equals(y)
        };
    }

    private static bool SequenceEquals(IEnumerable x, IEnumerable y)
    {
        var ex = x.GetEnumerator();
        var ey = y.GetEnumerator();
        try
        {
            while (true)
            {
                var hx = ex.MoveNext();
                var hy = ey.MoveNext();
                if (hx != hy)
                {
                    return false;
                }

                if (!hx)
                {
                    return true;
                }

                if (!ValueEquals(ex.Current, ey.Current))
                {
                    return false;
                }
            }
        }
        finally
        {
            (ex as IDisposable)?.Dispose();
            (ey as IDisposable)?.Dispose();
        }
    }
}

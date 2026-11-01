namespace HomeManager.Host.Application.Circuits;

using System.Collections.Concurrent;

// 接続中の回線(Blazor Server のサーキット)を利用者名つきで保持する
public sealed class CircuitTracker
{
    private readonly ConcurrentDictionary<string, CircuitInfo> circuits = new();

    public event EventHandler? Changed;

    public int Count => circuits.Count;

    public IReadOnlyList<CircuitInfo> List() =>
        circuits.Values.OrderBy(static x => x.OpenedAt).ToList();

    public void Add(CircuitInfo info)
    {
        circuits[info.Id] = info;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetConnected(string id, bool connected)
    {
        if (circuits.TryGetValue(id, out var info) && (info.Connected != connected))
        {
            circuits[id] = info with { Connected = connected };
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Remove(string id)
    {
        if (circuits.TryRemove(id, out _))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}

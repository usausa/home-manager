namespace HomeManager.Host.Components;

// 画面の描き直しの間引き。最初の要求はすぐに知らせ、続く要求は間隔ごとにまとめて知らせる
// (センサーが続けて値を送っても描き直しは間隔に1回。maui-server の RefreshTimer と違い、変化が無い間は動かない)
public sealed class RefreshThrottle : IDisposable
{
    private readonly Lock sync = new();

    private readonly TimeSpan interval;

    private readonly Func<Task> refresh;

    private readonly ITimer timer;

    private bool waiting;

    private bool requested;

    private bool disposed;

    public RefreshThrottle(TimeProvider timeProvider, TimeSpan interval, Func<Task> refresh)
    {
        this.interval = interval;
        this.refresh = refresh;
        timer = timeProvider.CreateTimer(_ => OnTimer(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        timer.Dispose();
    }

    public void Request()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            if (waiting)
            {
                requested = true;
                return;
            }

            waiting = true;
            timer.Change(interval, Timeout.InfiniteTimeSpan);
        }

        _ = NotifyAsync();
    }

    private void OnTimer()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            if (!requested)
            {
                waiting = false;
                return;
            }

            requested = false;
            timer.Change(interval, Timeout.InfiniteTimeSpan);
        }

        _ = NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        try
        {
            await refresh();
        }
        catch (ObjectDisposedException)
        {
            // 回線が閉じた
        }
    }
}

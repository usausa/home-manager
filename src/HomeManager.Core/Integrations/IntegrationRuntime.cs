namespace HomeManager.Integrations;

// 読み込んだインテグレーション1つ分の実行環境。バックグラウンドの処理の寿命を管理する
internal sealed class IntegrationRuntime : IIntegrationContext, IDisposable
{
    private readonly CancellationTokenSource cts = new();

    private readonly List<Task> tasks = [];

    private readonly ILogger log;

    private readonly EntityManager entityManager;

    public IntegrationRuntime(ILogger log, EntityManager entityManager, IIntegration integration, IntegrationAttribute metadata)
    {
        this.log = log;
        this.entityManager = entityManager;
        Integration = integration;
        Metadata = metadata;
    }

    public IIntegration Integration { get; }

    public IntegrationAttribute Metadata { get; }

    public string Domain => Metadata.Domain;

    public IntegrationStatus Status { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset? LoadedAt { get; set; }

    public void Dispose() => cts.Dispose();

    public IReadOnlyList<string> AddEntities(IEnumerable<HomeEntity> entities) =>
        entityManager.AddEntities(Domain, entities);

    public void RunBackground(Func<CancellationToken, Task> work)
    {
        var cancel = cts.Token;
        var task = Task.Run(
            async () =>
            {
                try
                {
                    await work(cancel);
                }
                catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                {
                    // Stopped
                }
                catch (Exception ex) when ((ex is not OperationCanceledException) || !cancel.IsCancellationRequested)
                {
                    log.ErrorIntegrationBackground(Domain, ex);
                }
            },
            CancellationToken.None);
        lock (tasks)
        {
            tasks.Add(task);
        }
    }

    public async ValueTask StopBackgroundAsync()
    {
        await cts.CancelAsync();

        Task[] pending;
        lock (tasks)
        {
            pending = [.. tasks];
            tasks.Clear();
        }

        await Task.WhenAll(pending);
    }
}

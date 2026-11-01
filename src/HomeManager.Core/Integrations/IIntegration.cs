namespace HomeManager.Integrations;

// インテグレーション (HA の __init__.py の async_setup)。1インテグレーション = 1プロジェクトで、DIに登録する
// メタ情報は IntegrationAttribute で付ける (HA の manifest.json)
public interface IIntegration
{
    ValueTask SetupAsync(IIntegrationContext context, CancellationToken cancel);

    ValueTask UnloadAsync(CancellationToken cancel) => ValueTask.CompletedTask;
}

namespace HomeManager.Integrations;

// セットアップのときにインテグレーションへ渡す窓口
public interface IIntegrationContext
{
    string Domain { get; }

    // エンティティを追加して最初の状態を書く (HA の async_add_entities)
    IReadOnlyList<string> AddEntities(IEnumerable<HomeEntity> entities);

    // 停止まで動く処理 (機器の監視など)。インテグレーションの停止でcancelが通知され、終わるまで待つ
    void RunBackground(Func<CancellationToken, Task> work);
}

namespace HomeManager.Services;

public interface ICoreConfigService
{
    // 今の全体設定 (版つき)
    CoreConfig Config { get; }

    // 全体設定のタイムゾーン (時刻の表示に使う)
    TimeZoneInfo TimeZone { get; }

    // DBから読み直す (起動時とhomemanager.reload_core_config)。変わっていればcore_config_updatedを出す
    ValueTask LoadAsync(EventContext context, CancellationToken cancellationToken = default);

    // 版が合えば保存してcore_config_updatedを出す (版が合わなければConflict)
    ValueTask<DataWriteStatus> UpdateAsync(CoreConfig value, EventContext context, CancellationToken cancellationToken = default);
}

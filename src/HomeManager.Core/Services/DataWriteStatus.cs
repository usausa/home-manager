namespace HomeManager.Services;

public enum DataWriteStatus
{
    Success,
    NotFound,
    Duplicate,
    // 楽観ロックの版が合わない (ほかの画面が先に変更した)
    Conflict
}

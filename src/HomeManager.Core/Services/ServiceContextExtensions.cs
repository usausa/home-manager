namespace HomeManager.Services;

public static class ServiceContextExtensions
{
    // 画面・APIの操作者を状態とイベントのContextに引き継ぐ (ログブックの「誰が」)
    public static EventContext CreateEventContext(this ServiceContextProvider provider) =>
        EventContext.Create(provider.Current.UserId);
}

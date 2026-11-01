namespace HomeManager.Services;

public sealed class SubscriptionOptions
{
    // 受け取り待ちの上限。nullは上限なし (記録・オートメーション向け)、上限ありは溢れたら古いものを捨てる (画面向け)
    public int? Capacity { get; init; }
}

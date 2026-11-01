namespace HomeManager.Domain;

// サービスの対象 (P1はentity_idだけ。P2でデバイス・エリアを足す)
public sealed record ServiceTarget(IReadOnlyList<string> EntityIds)
{
    public static ServiceTarget Empty { get; } = new([]);

    public static ServiceTarget Of(params IEnumerable<string> entityIds) => new([.. entityIds]);
}

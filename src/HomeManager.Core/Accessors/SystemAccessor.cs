namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class SystemAccessor
{
    [ExecuteScalar]
    public partial ValueTask<string> QueryVersionAsync(CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<string> QueryFileAsync(CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> QueryDatabaseSizeAsync(CancellationToken cancellationToken);
}

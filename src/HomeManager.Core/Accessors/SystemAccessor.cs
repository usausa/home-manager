namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class SystemAccessor
{
    [ExecuteScalar]
    public partial ValueTask<string> QueryServerVersionAsync(CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> QueryDatabaseSizeAsync(CancellationToken cancellationToken);
}

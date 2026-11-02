namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class SchemaAccessor
{
    [ExecuteScalar]
    public partial ValueTask<string> EnableWriteAheadLogAsync(DbConnection con, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> CreateVersionTableAsync(DbConnection con, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<SchemaVersionEntity>> QueryVersionListAsync(DbConnection con, CancellationToken cancellationToken);

    [DirectSql]
    [Execute]
    public partial ValueTask<int> ExecuteAsync(DbTransaction tx, string sql, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> InsertVersionAsync(DbTransaction tx, int version, string name, string checksum, DateTime appliedAt, CancellationToken cancellationToken);
}

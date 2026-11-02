namespace HomeManager.Services;

using System.Security.Cryptography;

using HomeManager.Accessors;

public sealed class DatabaseService
{
    private readonly IDbProvider provider;

    private readonly SchemaAccessor schemaAccessor;

    private readonly TimeProvider timeProvider;

    public DatabaseService(
        IDbProvider provider,
        SchemaAccessor schemaAccessor,
        TimeProvider timeProvider)
    {
        this.provider = provider;
        this.schemaAccessor = schemaAccessor;
        this.timeProvider = timeProvider;
    }

    // 番号付きのSQLファイル(0001_xxx.sql)を番号順に適用し、適用した版をSchemaVersionに記録する
    public async ValueTask MigrateAsync(string directory, CancellationToken cancellationToken = default)
    {
        var migrations = await LoadMigrationsAsync(directory, cancellationToken);

        await using var con = provider.CreateConnection();
        await con.OpenAsync(cancellationToken);

        // 書き込み中も読めるようにWALにする (DBファイルに残る設定。トランザクションの外で行う)
        await schemaAccessor.EnableWriteAheadLogAsync(con, cancellationToken);

        // アプリは1プロセスでDBファイルを持つため、マイグレーションのロックは取らない
        await schemaAccessor.CreateVersionTableAsync(con, cancellationToken);
        var applied = (await schemaAccessor.QueryVersionListAsync(con, cancellationToken)).ToDictionary(static x => x.Version);

        foreach (var migration in migrations)
        {
            if (applied.TryGetValue(migration.Version, out var entity))
            {
                // 適用済みのファイルが書き換えられていたら起動を止める
                if (entity.Checksum != migration.Checksum)
                {
                    throw new InvalidOperationException($"Applied migration was modified. name=[{migration.Name}]");
                }

                continue;
            }

            // 1ファイル=1トランザクション(SQLiteもDDLを巻き戻せる)
            await using var tx = await con.BeginTransactionAsync(cancellationToken);
            await schemaAccessor.ExecuteAsync(tx, migration.Sql, cancellationToken);
            await schemaAccessor.InsertVersionAsync(tx, migration.Version, migration.Name, migration.Checksum, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private static async ValueTask<List<Migration>> LoadMigrationsAsync(string directory, CancellationToken cancellationToken)
    {
        var list = new List<Migration>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.sql"))
        {
            var name = Path.GetFileName(path);
            var separator = name.IndexOf('_', StringComparison.Ordinal);
            if ((separator <= 0) || !Int32.TryParse(name.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var version))
            {
                throw new InvalidOperationException($"Invalid migration file name. name=[{name}]");
            }

            var sql = await File.ReadAllTextAsync(path, cancellationToken);
            // チェックアウトした環境の改行コードの違いでチェックサムが変わらないようにする
            var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql.ReplaceLineEndings("\n"))));
            list.Add(new Migration(version, name, sql, checksum));
        }

        list.Sort(static (x, y) => x.Version.CompareTo(y.Version));
        return list;
    }

    private sealed record Migration(int Version, string Name, string Sql, string Checksum);
}

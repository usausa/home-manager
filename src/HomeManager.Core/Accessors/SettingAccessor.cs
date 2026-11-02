namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class SettingAccessor
{
    [QueryFirst]
    public partial ValueTask<SettingEntity?> QueryByKeyAsync(string key, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> InsertAsync(string key, string value, DateTime updatedAt, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateAsync(string key, string value, DateTime updatedAt, int version, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpsertAsync(string key, string value, DateTime updatedAt, CancellationToken cancellationToken);
}

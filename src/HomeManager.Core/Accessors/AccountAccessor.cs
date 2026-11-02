namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class AccountAccessor
{
    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<AccountEntity?> QueryByNameAsync(string name, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, string displayName, byte[] password, string role, bool mustChangePassword, DateTime createdAt, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdatePasswordAsync(long id, byte[] password, DateTime updatedAt, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateProfileAsync(long id, string displayName, string preferences, DateTime updatedAt, int version, CancellationToken cancellationToken);
}

namespace HomeManager.Accessors;

[DataAccessor]
public sealed partial class AccountAccessor
{
    [ExecuteScalar]
    public partial ValueTask<int> CountAsync();

    [QueryFirst]
    public partial ValueTask<AccountEntity?> QueryByNameAsync(string name);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, string displayName, byte[] password, string role, bool mustChangePassword, DateTime createdAt);
}

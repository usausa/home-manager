namespace HomeManager.Services;

public interface IAccountService
{
    ValueTask<AccountEntity?> AuthenticateAsync(string name, string password, CancellationToken cancellationToken = default);

    ValueTask<AccountProfile?> FindProfileAsync(string name, CancellationToken cancellationToken = default);

    // 表示名と表示の設定を保存する (版が合わなければConflict)
    ValueTask<DataWriteStatus> UpdateProfileAsync(AccountProfile value, CancellationToken cancellationToken = default);

    // 今のパスワードを確かめてから変える。変えたら初回セットアップでの変更は不要になる
    ValueTask<PasswordChangeStatus> ChangePasswordAsync(string name, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

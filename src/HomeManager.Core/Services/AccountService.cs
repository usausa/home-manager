namespace HomeManager.Services;

using System.Text.Json;

using HomeManager.Accessors;
using HomeManager.Infrastructure.Security;
using HomeManager.Models.Entity;

public sealed class AccountService : IAccountService
{
    private readonly AccountAccessor accountAccessor;

    private readonly IPasswordProvider passwordProvider;

    private readonly TimeProvider timeProvider;

    public AccountService(
        AccountAccessor accountAccessor,
        IPasswordProvider passwordProvider,
        TimeProvider timeProvider)
    {
        this.accountAccessor = accountAccessor;
        this.passwordProvider = passwordProvider;
        this.timeProvider = timeProvider;
    }

    // Seed initial account
    public async ValueTask InitializeAsync(InitialAccountOption option, string role, CancellationToken cancellationToken = default)
    {
        var count = await accountAccessor.CountAsync(cancellationToken);
        if (count == 0)
        {
            // 初期ユーザーは初回セットアップでパスワードの変更を必須にする。時刻はUTCで書く
            await accountAccessor.InsertAsync(option.Id, option.Id, passwordProvider.Generate(option.Password), role, true, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        }
    }

    public async ValueTask<AccountEntity?> AuthenticateAsync(string name, string password, CancellationToken cancellationToken = default)
    {
        var account = await accountAccessor.QueryByNameAsync(name, cancellationToken);
        if ((account is null) || !account.IsEnabled)
        {
            return null;
        }

        return passwordProvider.Match(password, account.Password) ? account : null;
    }

    public async ValueTask<AccountProfile?> FindProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        var account = await accountAccessor.QueryByNameAsync(name, cancellationToken);
        return account is null
            ? null
            : new AccountProfile(account.Id, account.Name, account.DisplayName, account.Role, account.MustChangePassword, ParsePreferences(account.Preferences), account.Version);
    }

    public async ValueTask<DataWriteStatus> UpdateProfileAsync(AccountProfile value, CancellationToken cancellationToken = default)
    {
        var error = AccountRules.ValidateDisplayName(value.DisplayName);
        if (error is not null)
        {
            throw new ServiceValidationException(error);
        }

        var preferences = JsonSerializer.Serialize(NormalizePreferences(value.Preferences), StoredJson.Options);
        var count = await accountAccessor.UpdateProfileAsync(value.Id, value.DisplayName.Trim(), preferences, timeProvider.GetUtcNow().UtcDateTime, value.Version, cancellationToken);
        return count > 0 ? DataWriteStatus.Success : DataWriteStatus.Conflict;
    }

    public async ValueTask<PasswordChangeStatus> ChangePasswordAsync(string name, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (AccountRules.ValidatePassword(newPassword) is not null)
        {
            return PasswordChangeStatus.InvalidPassword;
        }

        var account = await accountAccessor.QueryByNameAsync(name, cancellationToken);
        if ((account is null) || !account.IsEnabled)
        {
            return PasswordChangeStatus.NotFound;
        }

        if (!passwordProvider.Match(currentPassword, account.Password))
        {
            return PasswordChangeStatus.WrongPassword;
        }

        // 今と同じパスワードには変えられない (初期パスワードのままにさせない)
        if (passwordProvider.Match(newPassword, account.Password))
        {
            return PasswordChangeStatus.InvalidPassword;
        }

        await accountAccessor.UpdatePasswordAsync(account.Id, passwordProvider.Generate(newPassword), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return PasswordChangeStatus.Success;
    }

    // 読めない値や知らないテーマは既定に戻す
    private static AccountPreferences ParsePreferences(string? json)
    {
        if (String.IsNullOrWhiteSpace(json))
        {
            return new AccountPreferences();
        }

        try
        {
            return NormalizePreferences(JsonSerializer.Deserialize<AccountPreferences>(json, StoredJson.Options) ?? new AccountPreferences());
        }
        catch (JsonException)
        {
            return new AccountPreferences();
        }
    }

    private static AccountPreferences NormalizePreferences(AccountPreferences value) =>
        ThemeModes.All.Contains(value.Theme) ? value : value with { Theme = ThemeModes.Auto };
}

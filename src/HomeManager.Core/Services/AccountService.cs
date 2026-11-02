namespace HomeManager.Services;

using System.Text.Json;

using HomeManager.Accessors;
using HomeManager.Infrastructure.Security;
using HomeManager.Models.Entity;

public sealed class AccountService : IAccountService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AccountAccessor accountAccessor;

    private readonly IPasswordProvider passwordProvider;

    private readonly ServiceContextProvider contextProvider;

    public AccountService(
        AccountAccessor accountAccessor,
        IPasswordProvider passwordProvider,
        ServiceContextProvider contextProvider)
    {
        this.accountAccessor = accountAccessor;
        this.passwordProvider = passwordProvider;
        this.contextProvider = contextProvider;
    }

    // Seed initial account
    public async ValueTask InitializeAsync(InitialAccountOption option, string role)
    {
        var count = await accountAccessor.CountAsync();
        if (count == 0)
        {
            var context = contextProvider.Current;
            // 初期ユーザーは初回セットアップでパスワードの変更を必須にする。TIMESTAMPTZの列にはUTCで書く
            await accountAccessor.InsertAsync(option.Id, option.Id, passwordProvider.Generate(option.Password), role, true, context.Now.UtcDateTime);
        }
    }

    public async ValueTask<AccountEntity?> AuthenticateAsync(string name, string password)
    {
        var account = await accountAccessor.QueryByNameAsync(name);
        if ((account is null) || !account.IsEnabled)
        {
            return null;
        }

        return passwordProvider.Match(password, account.Password) ? account : null;
    }

    public async ValueTask<AccountProfile?> FindProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        var account = await accountAccessor.QueryByNameAsync(name);
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

        var preferences = JsonSerializer.Serialize(NormalizePreferences(value.Preferences), JsonOptions);
        var count = await accountAccessor.UpdateProfileAsync(value.Id, value.DisplayName.Trim(), preferences, contextProvider.Current.Now.UtcDateTime, value.Version, cancellationToken);
        return count > 0 ? DataWriteStatus.Success : DataWriteStatus.Conflict;
    }

    public async ValueTask<PasswordChangeStatus> ChangePasswordAsync(string name, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (AccountRules.ValidatePassword(newPassword) is not null)
        {
            return PasswordChangeStatus.InvalidPassword;
        }

        var account = await accountAccessor.QueryByNameAsync(name);
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

        await accountAccessor.UpdatePasswordAsync(account.Id, passwordProvider.Generate(newPassword), contextProvider.Current.Now.UtcDateTime, cancellationToken);
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
            return NormalizePreferences(JsonSerializer.Deserialize<AccountPreferences>(json, JsonOptions) ?? new AccountPreferences());
        }
        catch (JsonException)
        {
            return new AccountPreferences();
        }
    }

    private static AccountPreferences NormalizePreferences(AccountPreferences value) =>
        ThemeModes.All.Contains(value.Theme) ? value : value with { Theme = ThemeModes.Auto };
}

namespace HomeManager.Services;

using HomeManager.Accessors;
using HomeManager.Infrastructure.Security;
using HomeManager.Models.Entity;

public sealed class AccountService
{
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
}

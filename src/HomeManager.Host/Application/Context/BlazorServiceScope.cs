namespace HomeManager.Host.Application.Context;

using Microsoft.AspNetCore.Components.Authorization;

// Blazor の境界 (回線単位)。実行ユーザーは回線の認証状態から取る
public sealed class BlazorServiceScope
{
    private readonly TimeProvider timeProvider;

    private readonly AuthenticationStateProvider authenticationStateProvider;

    private readonly ApplicationServiceContextProvider provider;

    public BlazorServiceScope(
        TimeProvider timeProvider,
        AuthenticationStateProvider authenticationStateProvider,
        ApplicationServiceContextProvider provider)
    {
        this.timeProvider = timeProvider;
        this.authenticationStateProvider = authenticationStateProvider;
        this.provider = provider;
    }

    // AsyncLocal は非同期メソッドの中で設定しても呼び出し元へ伝わらないため、ここでは値の生成だけを用意し、Begin は境界のメソッドが直接呼ぶ
    public async ValueTask<Func<ServiceContext>> PrepareAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = HttpServiceContext.GetUserId(state.User);
        return () => new ServiceContext(timeProvider.GetLocalNow(), userId);
    }

    public IDisposable Begin(Func<ServiceContext> factory) => provider.Begin(factory);
}

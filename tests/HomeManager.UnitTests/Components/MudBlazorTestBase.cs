namespace HomeManager.Components;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Application.Context;
using HomeManager.Services;

using Microsoft.Extensions.DependencyInjection;

using MudBlazor.Services;

using NSubstitute;

public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<ApplicationServiceContextProvider>();
        Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        Services.AddScoped<BlazorServiceScope>();

        // 時刻の表示は全体設定のタイムゾーン (東京) を使う
        CoreConfig = Substitute.For<ICoreConfigService>();
        CoreConfig.Config.Returns(new CoreConfig { Version = 1 });
        CoreConfig.TimeZone.Returns(TimeZones.Find(TimeZones.Default));
        Services.AddSingleton(CoreConfig);
    }

    protected ICoreConfigService CoreConfig { get; }
}

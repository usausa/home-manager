namespace HomeManager;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

// テストごとに作り、InitializeAsyncでPostgreSQLコンテナを起動してからサーバーを開始する
public sealed class E2EApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? container;

    public string ServerAddress
    {
        get
        {
            var server = Services.GetRequiredService<IServer>();
            return server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
        }
    }

    public async ValueTask InitializeAsync()
    {
        container = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await container.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", container?.GetConnectionString() ?? throw new InvalidOperationException("InitializeAsync is not called."));
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Notification:Enable", "false");
        builder.UseSetting("Profiler:SqlLog:Enable", "false");
        builder.UseSetting("Profiler:SqlTelemetry:Enable", "false");
        builder.UseSetting("Log:HttpLog", "false");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}

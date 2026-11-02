namespace HomeManager;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

// テストごとに作り、一時ファイルのSQLiteでサーバーを開始する (コンテナは要らない)
public sealed class E2EApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseFile = Path.Combine(Path.GetTempPath(), $"homemanager-e2e-{Guid.NewGuid():N}.db");

    public string ServerAddress
    {
        get
        {
            var server = Services.GetRequiredService<IServer>();
            return server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databaseFile};Foreign Keys=True;Pooling=False");
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Profiler:SqlLog:Enable", "false");
        builder.UseSetting("Profiler:SqlTelemetry:Enable", "false");
        builder.UseSetting("Log:HttpLog", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // WALの補助ファイル (-wal / -shm) も消す
            foreach (var file in new[] { databaseFile, databaseFile + "-wal", databaseFile + "-shm" })
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // Ignore
                }
            }
        }
    }
}

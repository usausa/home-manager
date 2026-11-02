namespace HomeManager;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

// テストのクラスごとに一時ファイルのSQLiteを使う (コンテナは要らない)
public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseFile = Path.Combine(Path.GetTempPath(), $"homemanager-test-{Guid.NewGuid():N}.db");

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

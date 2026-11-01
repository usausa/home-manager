namespace HomeManager;

using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

public abstract class E2ETestBase : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        Locale = "ja-JP",
        TimezoneId = "Asia/Tokyo",
        ColorScheme = ColorScheme.Light
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await Context.Tracing.StartAsync(new TracingStartOptions { Screenshots = true, Snapshots = true, Sources = true });
    }

    public override async ValueTask DisposeAsync()
    {
        var errorShown = await Page.Locator("#blazor-error-ui").IsVisibleAsync();
        await Context.Tracing.StopAsync(new TracingStopOptions { Path = TestOk() && !errorShown ? null : MakeTracePath() });
        await base.DisposeAsync();
        GC.SuppressFinalize(this);

        Assert.False(errorShown, "#blazor-error-ui が表示された");
    }

    private string MakeTracePath()
    {
        var name = TestContext.Current.Test?.TestDisplayName ?? GetType().Name;
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return Path.Combine(AppContext.BaseDirectory, "playwright-traces", $"{name}.zip");
    }
}

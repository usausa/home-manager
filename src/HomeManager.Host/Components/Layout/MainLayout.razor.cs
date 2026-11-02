namespace HomeManager.Host.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

// 上部バー・サイドバーのある画面の枠。初回セットアップが済むまでは中身を出さず、テーマはプロフィールの設定に合わせる
public sealed partial class MainLayout : IDisposable
{
    private MudThemeProvider? themeProvider;

    private ErrorBoundary? errorBoundary;

    private bool ready;

    private bool drawerOpen = true;

    private bool isDarkMode;

    private bool systemDarkMode;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required UserSession Session { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    private bool IsAutoTheme => Session.Theme == ThemeModes.Auto;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        Session.Changed += OnSessionChanged;

        await Session.LoadAsync();
        if (Session.IsOnboardingRequired)
        {
            Navigation.NavigateTo("onboarding", replace: true);
            return;
        }

        ready = true;
        UpdateDarkMode();
    }

    protected override void OnParametersSet() => errorBoundary?.Recover();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // OSのテーマは描画した後でないと取れない (テーマが自動のときに使う)
        if (firstRender && (themeProvider is not null))
        {
            systemDarkMode = await themeProvider.GetSystemDarkModeAsync();
            await themeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
            if (UpdateDarkMode())
            {
                StateHasChanged();
            }
        }
    }

    public void Dispose()
    {
        Session.Changed -= OnSessionChanged;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void ToggleDrawer() => drawerOpen = !drawerOpen;

    private void Recover() => errorBoundary?.Recover();

    private void OnSessionChanged(object? sender, EventArgs e) =>
        _ = InvokeAsync(async () =>
        {
            // 自動にしたときは今のOSのテーマを読み直す (自動でない間は見張っていない)
            if (IsAutoTheme && (themeProvider is not null))
            {
                systemDarkMode = await themeProvider.GetSystemDarkModeAsync();
            }

            UpdateDarkMode();
            StateHasChanged();
        });

    private Task OnSystemDarkModeChanged(bool value) =>
        InvokeAsync(() =>
        {
            systemDarkMode = value;
            if (UpdateDarkMode())
            {
                StateHasChanged();
            }
        });

    // 変わったらtrue
    private bool UpdateDarkMode()
    {
        var value = Session.Theme switch
        {
            ThemeModes.Dark => true,
            ThemeModes.Light => false,
            _ => systemDarkMode
        };
        if (value == isDarkMode)
        {
            return false;
        }

        isDarkMode = value;
        return true;
    }
}

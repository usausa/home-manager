namespace HomeManager.Components.Layout;

using Bunit;

using HomeManager.Domain;
using HomeManager.Host.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MudBlazor;
using MudBlazor.Extensions;

using NSubstitute;

public sealed class MainLayoutTests : SessionTestBase
{
    private const string Body = "<p>本文</p>";

    [Fact]
    public void InitialPasswordRedirectsToOnboarding()
    {
        // Arrange
        SignIn(CreateProfile(mustChangePassword: true));
        var navigation = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = Render<MainLayout>(parameters => parameters.Add(static x => x.Body, Body));

        // Assert
        cut.WaitForAssertion(() => Assert.EndsWith("/onboarding", navigation.Uri, StringComparison.Ordinal));
        Assert.DoesNotContain("本文", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreConfigNotDoneRedirectsAdministrator()
    {
        // Arrange
        SignIn(CreateProfile());
        Onboarding.IsDone(OnboardingSteps.CoreConfig).Returns(false);
        var navigation = Services.GetRequiredService<NavigationManager>();

        // Act
        Render<MainLayout>(parameters => parameters.Add(static x => x.Body, Body));

        // Assert
        Assert.EndsWith("/onboarding", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowsBodyWithDisplayNameAndTheme()
    {
        // Arrange
        SignIn(CreateProfile() with { DisplayName = "まち", Preferences = new AccountPreferences { Theme = ThemeModes.Dark } });

        // Act
        var cut = Render<MainLayout>(parameters => parameters.Add(static x => x.Body, Body));

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("本文", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("まち", cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.FindComponent<MudThemeProvider>().Instance.GetState(static x => x.IsDarkMode));
    }

    // テーマが自動のときだけOSのテーマを見張り、開いたままの切り替えにも追従する
    [Fact]
    public async Task AutoThemeFollowsSystem()
    {
        // Arrange
        JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", static _ => true).SetResult(true);
        SignIn(CreateProfile());
        var cut = Render<MainLayout>(parameters => parameters.Add(static x => x.Body, Body));
        var provider = cut.FindComponent<MudThemeProvider>();
        cut.WaitForAssertion(() => Assert.True(provider.Instance.GetState(static x => x.IsDarkMode)));

        // Act
        await cut.InvokeAsync(() => provider.Instance.SystemDarkModeChangedAsync(false));

        // Assert
        Assert.True(provider.Instance.GetState(static x => x.ObserveSystemDarkModeChange));
        cut.WaitForAssertion(() => Assert.False(provider.Instance.GetState(static x => x.IsDarkMode)));
    }

    [Fact]
    public void ExplicitThemeDoesNotWatchSystem()
    {
        // Arrange
        JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", static _ => true).SetResult(true);
        SignIn(CreateProfile() with { Preferences = new AccountPreferences { Theme = ThemeModes.Light } });

        // Act
        var cut = Render<MainLayout>(parameters => parameters.Add(static x => x.Body, Body));

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("本文", cut.Markup, StringComparison.Ordinal));
        var provider = cut.FindComponent<MudThemeProvider>();
        Assert.False(provider.Instance.GetState(static x => x.ObserveSystemDarkModeChange));
        Assert.False(provider.Instance.GetState(static x => x.IsDarkMode));
    }
}

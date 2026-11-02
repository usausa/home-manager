namespace HomeManager.Components;

using System.Security.Claims;

using HomeManager.Domain;
using HomeManager.Host.Application;
using HomeManager.Host.Components;
using HomeManager.Services;

using Microsoft.AspNetCore.Components.Authorization;

using NSubstitute;

public sealed class UserSessionTests
{
    private readonly IAccountService accountService = Substitute.For<IAccountService>();

    private readonly IOnboardingService onboardingService = Substitute.For<IOnboardingService>();

    [Fact]
    public async Task AnonymousDoesNotNeedOnboarding()
    {
        // Arrange
        var session = CreateSession(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await session.LoadAsync();

        // Assert
        Assert.Null(session.Profile);
        Assert.False(session.IsOnboardingRequired);
        await accountService.DidNotReceiveWithAnyArgs().FindProfileAsync(default!, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, true)]
    public async Task OnboardingIsRequiredUntilPasswordAndCoreConfigAreDone(bool administrator, bool mustChangePassword, bool coreConfigDone, bool expected)
    {
        // Arrange
        var role = administrator ? Roles.Administrator : "User";
        accountService.FindProfileAsync("machi", Arg.Any<CancellationToken>())
            .Returns(new AccountProfile(1, "machi", "まち", role, mustChangePassword, new AccountPreferences(), 1));
        onboardingService.IsDone(OnboardingSteps.CoreConfig).Returns(coreConfigDone);
        var session = CreateSession(CreateUser("machi", role));

        // Act
        await session.LoadAsync();

        // Assert
        Assert.Equal(expected, session.IsOnboardingRequired);
        Assert.Equal("まち", session.DisplayName);
    }

    [Fact]
    public async Task LoadReadsOnceAndReloadNotifies()
    {
        // Arrange
        accountService.FindProfileAsync("admin", Arg.Any<CancellationToken>())
            .Returns(
                new AccountProfile(1, "admin", "admin", Roles.Administrator, false, new AccountPreferences(), 1),
                new AccountProfile(1, "admin", "admin", Roles.Administrator, false, new AccountPreferences { Theme = ThemeModes.Dark }, 2));
        var session = CreateSession(CreateUser("admin", Roles.Administrator));
        var changed = 0;
        session.Changed += (_, _) => changed++;

        // Act
        await session.LoadAsync();
        await session.LoadAsync();
        var before = session.Theme;
        await session.ReloadAsync();

        // Assert
        Assert.Equal(ThemeModes.Auto, before);
        Assert.Equal(ThemeModes.Dark, session.Theme);
        Assert.Equal(2, changed);
        await accountService.Received(2).FindProfileAsync("admin", Arg.Any<CancellationToken>());
    }

    private UserSession CreateSession(ClaimsPrincipal user) =>
        new(new FixedAuthenticationStateProvider(user), accountService, onboardingService);

    private static ClaimsPrincipal CreateUser(string name, string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Role, role)], "Test"));

    private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}

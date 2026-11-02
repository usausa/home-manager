namespace HomeManager.Host.Endpoints;

using Microsoft.AspNetCore.Authentication.Cookies;

public static class AuthEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", HandleLoginAsync)
            .AllowAnonymous()
            .WithName("AuthLogin")
            .Produces(StatusCodes.Status302Found);
        group.MapPost("/logout", HandleLogoutAsync)
            .RequireAuthorization()
            .WithName("AuthLogout")
            .Produces(StatusCodes.Status302Found);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleLoginAsync(
        HttpContext context,
        IAccountService accountService,
        [FromForm] string? name,
        [FromForm] string? password,
        [FromForm] string? returnUrl)
    {
        var account = String.IsNullOrEmpty(name) || String.IsNullOrEmpty(password)
            ? null
            : await accountService.AuthenticateAsync(name, password);
        if (account is null)
        {
            return TypedResults.LocalRedirect($"~/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? string.Empty)}");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, account.Name),
                new Claim(ClaimTypes.Name, account.Name),
                new Claim(ClaimTypes.Role, account.Role)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Normalize to base relative (prevent protocol-relative redirect)
        var target = returnUrl?.TrimStart('/');
        return TypedResults.LocalRedirect(String.IsNullOrEmpty(target) ? "~/" : "~/" + target);
    }

    private static async ValueTask<IResult> HandleLogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return TypedResults.LocalRedirect("~/login");
    }
}

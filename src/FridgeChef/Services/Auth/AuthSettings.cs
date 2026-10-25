using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace FridgeChef.Services.Auth;

/// <summary>Whether sign-in is configured (AzureAd:ClientId set). Off for demo mode and anonymous local dev.</summary>
public sealed record AuthSettings(bool Enabled, string? ApiScope = null)
{
    public const string DefaultScopeName = "access_as_user";

    public static AuthSettings From(IConfiguration configuration)
    {
        var clientId = configuration["AzureAd:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return new AuthSettings(false);
        }

        var scope = configuration["Api:Scope"];
        return new AuthSettings(true, string.IsNullOrWhiteSpace(scope) ? $"api://{clientId}/{DefaultScopeName}" : scope);
    }
}

/// <summary>Used when sign-in isn't configured, so AuthorizeView and friends still work.</summary>
public sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> Anonymous = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Anonymous;
}

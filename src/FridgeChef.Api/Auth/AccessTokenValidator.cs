using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FridgeChef.Api.Auth;

public sealed record AuthResult(bool Allowed, int Status, string? Problem = null, string? ObjectId = null)
{
    public static AuthResult Ok(string? objectId) => new(true, 200, null, objectId);
    public static AuthResult Unauthorized(string problem) => new(false, 401, problem);
    public static AuthResult Forbidden(string problem, string? objectId) => new(false, 403, problem, objectId);
}

public interface IAccessTokenValidator
{
    Task<AuthResult> ValidateAsync(string? authorizationHeader, CancellationToken cancellationToken);
}

/// <summary>
/// Checks the bearer token: signed by our tenant, for our app, with the API scope, and from an
/// allow-listed object ID. Anything else is refused before a function runs.
/// </summary>
public sealed class AccessTokenValidator(
    IOptions<AuthOptions> options,
    IConfigurationManager<OpenIdConnectConfiguration> signingConfiguration,
    IHostEnvironment environment,
    ILogger<AccessTokenValidator> logger) : IAccessTokenValidator
{
    private readonly AuthOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public static string MetadataAddress(string tenantId) =>
        $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

    public async Task<AuthResult> ValidateAsync(string? authorizationHeader, CancellationToken cancellationToken)
    {
        if (_options.AllowAnonymousInDevelopment && environment.IsDevelopment())
        {
            return AuthResult.Ok(null);
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthResult.Unauthorized("Sign in to use Fridge Chef's AI features.");
        }

        var config = await signingConfiguration.GetConfigurationAsync(cancellationToken);
        var result = await _handler.ValidateTokenAsync(authorizationHeader["Bearer ".Length..].Trim(), new TokenValidationParameters
        {
            // v2 tokens (accessTokenAcceptedVersion: 2) and v1 tokens use different issuer and audience forms.
            ValidIssuers =
            [
                $"https://login.microsoftonline.com/{_options.TenantId}/v2.0",
                $"https://sts.windows.net/{_options.TenantId}/",
            ],
            ValidAudiences = [_options.ClientId, $"api://{_options.ClientId}"],
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        });

        if (!result.IsValid)
        {
            logger.LogWarning(result.Exception, "Rejected access token");
            return AuthResult.Unauthorized("Your sign-in has expired or isn't valid. Sign in again.");
        }

        var identity = result.ClaimsIdentity;
        var objectId = identity.FindFirst("oid")?.Value;
        var scopes = (identity.FindFirst("scp")?.Value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (!scopes.Contains(_options.RequiredScope, StringComparer.Ordinal))
        {
            return AuthResult.Forbidden("The sign-in doesn't include access to this API.", objectId);
        }

        if (objectId is null || !_options.AllowedObjectIds.Contains(objectId, StringComparer.OrdinalIgnoreCase))
        {
            // Logged so the right ID can be added to Auth__AllowedObjectIds during setup.
            logger.LogWarning("Refused signed-in user {ObjectId}: not on the allow-list", objectId);
            return AuthResult.Forbidden("This account isn't allowed to use Fridge Chef.", objectId);
        }

        return AuthResult.Ok(objectId);
    }
}

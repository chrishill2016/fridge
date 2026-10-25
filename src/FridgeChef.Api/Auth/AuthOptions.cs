using System.ComponentModel.DataAnnotations;

namespace FridgeChef.Api.Auth;

/// <summary>Bound from the "Auth" config section (app settings use Auth__TenantId etc.).</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Entra tenant the app registration lives in. Only tokens from here are accepted.</summary>
    [Required]
    public string TenantId { get; set; } = "";

    /// <summary>The app registration's client ID: the token audience.</summary>
    [Required]
    public string ClientId { get; set; } = "";

    /// <summary>Delegated scope the app must have asked for.</summary>
    public string RequiredScope { get; set; } = "access_as_user";

    /// <summary>Object IDs (oid claim) of the people allowed to call the API. Empty means nobody.</summary>
    public string[] AllowedObjectIds { get; set; } = [];

    /// <summary>
    /// Skips token checks, but only when the Functions host runs in Development (func start).
    /// Lets the API be tried locally before the app registration exists. Ignored in Azure.
    /// </summary>
    public bool AllowAnonymousInDevelopment { get; set; }
}

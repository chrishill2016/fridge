namespace FridgeChef.Services.Api;

/// <summary>Bound from the "Api" section of wwwroot/appsettings*.json.</summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>Base URL of the Function, ending in "/api/". Empty means demo mode.</summary>
    public string? BaseUrl { get; set; }
}

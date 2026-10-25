namespace FridgeChef.Models;

/// <summary>Device-only settings that are never sent to the API.</summary>
/// <param name="Model">Model deployment chosen in Settings; null lets the API use its defaults.</param>
public sealed record AppSettings(bool DarkMode = true, string? Model = null)
{
    public static AppSettings Default { get; } = new();
}

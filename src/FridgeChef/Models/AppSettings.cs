namespace FridgeChef.Models;

/// <summary>Device-only settings that are never sent to the API.</summary>
public sealed record AppSettings(bool DarkMode = true)
{
    public static AppSettings Default { get; } = new();
}

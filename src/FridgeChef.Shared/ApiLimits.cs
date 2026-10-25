namespace FridgeChef.Shared;

/// <summary>Request limits enforced by the API. The app uses them to check (and shrink) photos before upload.</summary>
public static class ApiLimits
{
    public const long MaxImageBytes = 4 * 1024 * 1024;

    public static IReadOnlyList<string> ImageMediaTypes { get; } = ["image/jpeg", "image/png", "image/webp"];

    public const int MaxJsonBytes = 32 * 1024;
    public const int MaxIngredients = 60;
    public const int MaxListItems = 30;
    public const int MaxDislikes = 20;
    public const int MaxTextLength = 80;
}

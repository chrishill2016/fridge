namespace FridgeChef.Shared.Models;

/// <summary>User preferences sent with every recipe request.</summary>
public sealed record RecipePreferences(
    IReadOnlyList<string> Dislikes,
    bool QuickWeeknight = false,
    int Servings = RecipePreferences.DefaultServings)
{
    public const int DefaultServings = 2;
    public const int MinServings = 1;
    public const int MaxServings = 12;

    public static RecipePreferences Default { get; } = new([]);
}

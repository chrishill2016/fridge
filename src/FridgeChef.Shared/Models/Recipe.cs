namespace FridgeChef.Shared.Models;

public sealed record Recipe(
    string Title,
    int TimeMinutes,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Steps);

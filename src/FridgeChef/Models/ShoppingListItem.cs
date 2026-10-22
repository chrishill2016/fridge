namespace FridgeChef.Models;

public sealed record ShoppingListItem(
    string Id,
    string Name,
    bool Bought,
    DateTimeOffset AddedUtc,
    string? FromRecipe = null);

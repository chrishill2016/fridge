using FridgeChef.Models;

namespace FridgeChef.Services;

public interface IShoppingListService
{
    /// <summary>Items still to buy first (oldest first), then bought items.</summary>
    Task<IReadOnlyList<ShoppingListItem>> GetItemsAsync();

    /// <summary>Adds an item unless an unbought item with the same name exists. Returns null for blank names.</summary>
    Task<ShoppingListItem?> AddAsync(string name, string? fromRecipe = null);

    Task AddRangeAsync(IEnumerable<string> names, string? fromRecipe = null);
    Task ToggleBoughtAsync(string id);
    Task RemoveAsync(string id);
    Task ClearBoughtAsync();
}

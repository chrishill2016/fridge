using FridgeChef.Models;
using FridgeChef.Services.Storage;

namespace FridgeChef.Services;

public sealed class ShoppingListService(ILocalStore store, TimeProvider time) : IShoppingListService
{
    public async Task<IReadOnlyList<ShoppingListItem>> GetItemsAsync()
    {
        var items = await store.GetAllAsync<ShoppingListItem>(StoreNames.ShoppingList);
        return [.. items.OrderBy(i => i.Bought).ThenBy(i => i.AddedUtc)];
    }

    public async Task<ShoppingListItem?> AddAsync(string name, string? fromRecipe = null)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var existing = (await store.GetAllAsync<ShoppingListItem>(StoreNames.ShoppingList))
            .FirstOrDefault(i => string.Equals(i.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        if (existing is { Bought: false })
        {
            return existing;
        }

        // A bought item with the same name is put back on the list rather than duplicated.
        var item = existing is not null
            ? existing with { Bought = false, AddedUtc = time.GetUtcNow(), FromRecipe = fromRecipe ?? existing.FromRecipe }
            : new ShoppingListItem(Guid.NewGuid().ToString("N"), trimmed, false, time.GetUtcNow(), fromRecipe);

        await store.PutAsync(StoreNames.ShoppingList, item.Id, item);
        return item;
    }

    public async Task AddRangeAsync(IEnumerable<string> names, string? fromRecipe = null)
    {
        foreach (var name in names)
        {
            await AddAsync(name, fromRecipe);
        }
    }

    public async Task ToggleBoughtAsync(string id)
    {
        var item = await store.GetAsync<ShoppingListItem>(StoreNames.ShoppingList, id);
        if (item is not null)
        {
            await store.PutAsync(StoreNames.ShoppingList, id, item with { Bought = !item.Bought });
        }
    }

    public Task RemoveAsync(string id) => store.DeleteAsync(StoreNames.ShoppingList, id);

    public async Task ClearBoughtAsync()
    {
        foreach (var item in await store.GetAllAsync<ShoppingListItem>(StoreNames.ShoppingList))
        {
            if (item.Bought)
            {
                await store.DeleteAsync(StoreNames.ShoppingList, item.Id);
            }
        }
    }
}

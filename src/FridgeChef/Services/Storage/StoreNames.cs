namespace FridgeChef.Services.Storage;

/// <summary>IndexedDB object store names. Must match STORES in wwwroot/js/localStore.js.</summary>
public static class StoreNames
{
    public const string Settings = "settings";
    public const string ShoppingList = "shoppingList";
    public const string SavedRecipes = "savedRecipes";

    public static IReadOnlySet<string> All { get; } = new HashSet<string> { Settings, ShoppingList, SavedRecipes };

    public static void EnsureValid(string store)
    {
        if (!All.Contains(store))
        {
            throw new ArgumentException($"Unknown store '{store}'.", nameof(store));
        }
    }
}

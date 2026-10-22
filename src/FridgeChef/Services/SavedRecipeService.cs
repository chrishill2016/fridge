using FridgeChef.Models;
using FridgeChef.Services.Storage;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

public sealed class SavedRecipeService(ILocalStore store, TimeProvider time) : ISavedRecipeService
{
    public async Task<IReadOnlyList<SavedRecipe>> GetAllAsync()
    {
        var saved = await store.GetAllAsync<SavedRecipe>(StoreNames.SavedRecipes);
        return [.. saved.OrderByDescending(r => r.SavedUtc)];
    }

    public async Task<SavedRecipe> SaveAsync(Recipe recipe)
    {
        var existing = await FindByTitleAsync(recipe.Title);
        var saved = new SavedRecipe(existing?.Id ?? Guid.NewGuid().ToString("N"), recipe, time.GetUtcNow());
        await store.PutAsync(StoreNames.SavedRecipes, saved.Id, saved);
        return saved;
    }

    public async Task<bool> IsSavedAsync(string title) => await FindByTitleAsync(title) is not null;

    public Task RemoveAsync(string id) => store.DeleteAsync(StoreNames.SavedRecipes, id);

    private async Task<SavedRecipe?> FindByTitleAsync(string title) =>
        (await store.GetAllAsync<SavedRecipe>(StoreNames.SavedRecipes))
            .FirstOrDefault(r => string.Equals(r.Recipe.Title.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase));
}

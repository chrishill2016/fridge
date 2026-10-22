using FridgeChef.Models;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

public interface ISavedRecipeService
{
    /// <summary>Saved recipes, newest first.</summary>
    Task<IReadOnlyList<SavedRecipe>> GetAllAsync();

    /// <summary>Saves a recipe. A recipe with the same title replaces the earlier one.</summary>
    Task<SavedRecipe> SaveAsync(Recipe recipe);

    Task<bool> IsSavedAsync(string title);
    Task RemoveAsync(string id);
}

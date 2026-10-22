using FridgeChef.Models;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

public interface ISettingsService
{
    event Action<AppSettings>? AppSettingsChanged;

    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);

    Task<RecipePreferences> GetPreferencesAsync();

    /// <summary>Normalises (trims, de-duplicates dislikes, clamps servings) and saves. Returns what was stored.</summary>
    Task<RecipePreferences> SavePreferencesAsync(RecipePreferences preferences);
}

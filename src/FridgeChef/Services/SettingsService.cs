using FridgeChef.Models;
using FridgeChef.Services.Storage;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

public sealed class SettingsService(ILocalStore store) : ISettingsService
{
    internal const string AppSettingsKey = "app";
    internal const string PreferencesKey = "preferences";

    public event Action<AppSettings>? AppSettingsChanged;

    public async Task<AppSettings> GetAppSettingsAsync() =>
        await store.GetAsync<AppSettings>(StoreNames.Settings, AppSettingsKey) ?? AppSettings.Default;

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        await store.PutAsync(StoreNames.Settings, AppSettingsKey, settings);
        AppSettingsChanged?.Invoke(settings);
    }

    public async Task<RecipePreferences> GetPreferencesAsync() =>
        await store.GetAsync<RecipePreferences>(StoreNames.Settings, PreferencesKey) ?? RecipePreferences.Default;

    public async Task<RecipePreferences> SavePreferencesAsync(RecipePreferences preferences)
    {
        var normalised = Normalise(preferences);
        await store.PutAsync(StoreNames.Settings, PreferencesKey, normalised);
        return normalised;
    }

    internal static RecipePreferences Normalise(RecipePreferences preferences) => preferences with
    {
        Dislikes = [.. (preferences.Dislikes ?? [])
            .Select(d => d.Trim())
            .Where(d => d.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)],
        Servings = Math.Clamp(preferences.Servings, RecipePreferences.MinServings, RecipePreferences.MaxServings),
    };
}

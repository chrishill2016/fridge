using FridgeChef.Services;
using FridgeChef.Services.Browser;
using FridgeChef.Services.Storage;

namespace FridgeChef;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFridgeChef(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<ILocalStore, IndexedDbLocalStore>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IShoppingListService, ShoppingListService>();
        services.AddSingleton<ISavedRecipeService, SavedRecipeService>();
        return services;
    }
}

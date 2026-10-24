using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Services.Browser;
using FridgeChef.Services.Storage;

namespace FridgeChef;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFridgeChef(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<ILocalStore, IndexedDbLocalStore>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IShoppingListService, ShoppingListService>();
        services.AddSingleton<ISavedRecipeService, SavedRecipeService>();
        services.AddSingleton<ScanSession>();
        services.AddSingleton<IPhotoPreparer, BrowserPhotoPreparer>();
        services.AddFridgeChefApi(configuration);
        return services;
    }

    private static void AddFridgeChefApi(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>()?.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            services.AddSingleton<IFridgeChefApi, DemoFridgeChefApi>();
            return;
        }

        // Phase 5 adds a token handler here.
        services.AddSingleton<IFridgeChefApi>(_ => new HttpFridgeChefApi(new HttpClient { BaseAddress = new Uri(baseUrl) }));
    }
}

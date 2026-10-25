using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Services.Auth;
using FridgeChef.Services.Browser;
using FridgeChef.Services.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

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
        services.AddScoped<ShoppingListAssistant>();
        services.AddSingleton<IPhotoPreparer, BrowserPhotoPreparer>();

        var auth = AuthSettings.From(configuration);
        services.AddSingleton(auth);
        services.AddFridgeChefAuth(configuration, auth);
        services.AddFridgeChefApi(configuration, auth);
        return services;
    }

    private static void AddFridgeChefAuth(this IServiceCollection services, IConfiguration configuration, AuthSettings auth)
    {
        services.AddCascadingAuthenticationState();
        if (!auth.Enabled)
        {
            services.AddAuthorizationCore();
            services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
            return;
        }

        // Single-tenant app registration; the user signs in with their Microsoft account.
        services.AddMsalAuthentication(options =>
        {
            configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            options.ProviderOptions.DefaultAccessTokenScopes.Add(auth.ApiScope!);
            options.ProviderOptions.LoginMode = "redirect";
            // Keep the sign-in when the installed PWA is closed and reopened.
            options.ProviderOptions.Cache.CacheLocation = "localStorage";
        });
    }

    private static void AddFridgeChefApi(this IServiceCollection services, IConfiguration configuration, AuthSettings auth)
    {
        var baseUrl = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>()?.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            services.AddSingleton<IFridgeChefApi, DemoFridgeChefApi>();
            return;
        }

        var client = services.AddHttpClient<IFridgeChefApi, HttpFridgeChefApi>(http => http.BaseAddress = new Uri(baseUrl));
        if (auth.Enabled)
        {
            // Attaches the access token to calls to the API (and only the API).
            client.AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
                .ConfigureHandler(authorizedUrls: [baseUrl], scopes: [auth.ApiScope!]));
        }
    }
}

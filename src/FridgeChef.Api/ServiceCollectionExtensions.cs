using Azure.AI.Projects;
using Azure.Identity;
using FridgeChef.Api.Auth;
using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Features.Recipes;
using FridgeChef.Api.Features.ShoppingList;
using FridgeChef.Api.Foundry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FridgeChef.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFridgeChefApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // DefaultAzureCredential: "az login" locally, managed identity in Azure. No keys.
        services.AddSingleton(sp => new AIProjectClient(
            sp.GetRequiredService<IOptions<FoundryOptions>>().Value.ProjectEndpoint,
            new DefaultAzureCredential()));

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Fetches and caches the tenant's signing keys, refreshing them as Entra rotates them.
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                AccessTokenValidator.MetadataAddress(sp.GetRequiredService<IOptions<AuthOptions>>().Value.TenantId),
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true }));
        services.AddSingleton<IAccessTokenValidator, AccessTokenValidator>();

        services.AddSingleton<IModelSelector, ModelSelector>();
        services.AddSingleton<IChatClientProvider, FoundryChatClientProvider>();
        services.AddSingleton<IIngredientDetector, IngredientDetector>();
        services.AddSingleton<IRecipeGenerator, RecipeGenerator>();
        services.AddSingleton<IShoppingListAgent, ShoppingListAgent>();
        return services;
    }
}

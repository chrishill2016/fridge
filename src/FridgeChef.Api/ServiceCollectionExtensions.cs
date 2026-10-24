using Azure.AI.Projects;
using Azure.Identity;
using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Foundry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddSingleton<IModelSelector, ModelSelector>();
        services.AddSingleton<IChatClientProvider, FoundryChatClientProvider>();
        services.AddSingleton<IIngredientDetector, IngredientDetector>();
        return services;
    }
}

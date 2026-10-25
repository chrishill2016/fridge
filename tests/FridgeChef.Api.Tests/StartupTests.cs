using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Foundry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FridgeChef.Api.Tests;

public class StartupTests
{
    [Fact]
    public void Services_resolve_from_configuration()
    {
        using var provider = Build(new()
        {
            ["Foundry:ProjectEndpoint"] = "https://test-ai.services.ai.azure.com/api/projects/test-project",
            ["Foundry:IngredientsModel"] = "gpt-4.1-mini",
            ["Foundry:RecipesModel"] = "gpt-4.1-mini",
            ["Foundry:AllowedModels:0"] = "phi-4",
            ["Auth:TenantId"] = "tenant",
            ["Auth:ClientId"] = "client",
        });

        Assert.NotNull(provider.GetRequiredService<IIngredientDetector>());
        Assert.NotNull(provider.GetRequiredService<FridgeChef.Api.Auth.IAccessTokenValidator>());
        Assert.NotNull(provider.GetRequiredService<IChatClientProvider>().GetChatClient("gpt-4.1-mini"));
        Assert.Equal(["gpt-4.1-mini", "phi-4"], provider.GetRequiredService<IModelSelector>().AllowedModels);
    }

    [Fact]
    public void Missing_endpoint_fails_validation()
    {
        using var provider = Build(new() { ["Foundry:IngredientsModel"] = "gpt-4.1-mini", ["Foundry:RecipesModel"] = "gpt-4.1-mini", ["Auth:TenantId"] = "t", ["Auth:ClientId"] = "c" });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FoundryOptions>>().Value);
        Assert.Contains(nameof(FoundryOptions.ProjectEndpoint), ex.Message);
    }

    [Fact]
    public void Missing_auth_settings_fail_validation()
    {
        using var provider = Build(new()
        {
            ["Foundry:ProjectEndpoint"] = "https://test-ai.services.ai.azure.com/api/projects/test-project",
            ["Foundry:IngredientsModel"] = "gpt-4.1-mini",
            ["Foundry:RecipesModel"] = "gpt-4.1-mini",
        });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FridgeChef.Api.Auth.AuthOptions>>().Value);
        Assert.Contains("TenantId", ex.Message);
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Production" });
        services.AddFridgeChefApi(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}

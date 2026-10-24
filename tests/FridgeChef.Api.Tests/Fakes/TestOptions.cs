using FridgeChef.Api.Foundry;
using Microsoft.Extensions.Options;

namespace FridgeChef.Api.Tests.Fakes;

public static class TestOptions
{
    public static IOptions<FoundryOptions> Foundry(params string[] extraModels) => Options.Create(new FoundryOptions
    {
        ProjectEndpoint = new Uri("https://test-ai.services.ai.azure.com/api/projects/test-project"),
        IngredientsModel = "gpt-4.1-mini",
        RecipesModel = "gpt-4.1",
        AllowedModels = extraModels,
    });
}

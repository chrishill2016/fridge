using FridgeChef.Api.Foundry;
using FridgeChef.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace FridgeChef.Api.Features.Models;

/// <summary>Lists the deployments the app's model picker may offer.</summary>
public sealed class ModelsFunction(IModelSelector models, IOptions<FoundryOptions> options)
{
    // TODO(phase 5): require a valid token for the allow-listed account.
    [Function("models")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "models")] HttpRequest req) =>
        new OkObjectResult(new ModelsResponse([.. models.AllowedModels], options.Value.IngredientsModel, options.Value.RecipesModel));
}

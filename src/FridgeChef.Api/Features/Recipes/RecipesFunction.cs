using FridgeChef.Api.Foundry;
using FridgeChef.Api.Http;
using FridgeChef.Shared;
using FridgeChef.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Api.Features.Recipes;

public sealed class RecipesFunction(IRecipeGenerator generator, IModelSelector models, ILogger<RecipesFunction> logger)
{
    [Function("recipes")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "recipes")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var (request, problem) = await JsonBody.ReadAsync<RecipesRequest>(req, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var ingredients = TextList.Clean(request!.Ingredients, ApiLimits.MaxIngredients);
        if (ingredients.Count == 0)
        {
            return Problems.Create(StatusCodes.Status400BadRequest, "Add at least one ingredient.");
        }

        var given = request.Preferences ?? RecipePreferences.Default;
        var preferences = given with
        {
            Dislikes = TextList.Clean(given.Dislikes, ApiLimits.MaxDislikes),
            Servings = Math.Clamp(given.Servings, RecipePreferences.MinServings, RecipePreferences.MaxServings),
        };

        string model;
        try
        {
            model = models.Resolve(AiTask.Recipes, request.Model);
        }
        catch (ModelNotAllowedException ex)
        {
            return Problems.Create(StatusCodes.Status400BadRequest, ex.Message);
        }

        try
        {
            var result = await generator.SuggestAsync(ingredients, preferences, model, cancellationToken);
            logger.LogInformation(
                "Suggested {Count} recipes from {Ingredients} ingredients with {Model} ({PromptVersion}), {InputTokens} in / {OutputTokens} out tokens",
                result.Recipes.Count, ingredients.Count, model, result.Run.PromptVersion, result.Run.InputTokens, result.Run.OutputTokens);
            return result.Recipes.Count == 0
                ? Problems.Create(StatusCodes.Status502BadGateway, "The AI service didn't come up with any recipes. Try again.")
                : new OkObjectResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Recipe suggestion failed with {Model}", model);
            return Problems.Create(StatusCodes.Status502BadGateway, "The AI service couldn't suggest recipes. Try again shortly.");
        }
    }
}

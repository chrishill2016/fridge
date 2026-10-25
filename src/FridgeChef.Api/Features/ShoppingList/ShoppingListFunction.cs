using FridgeChef.Api.Foundry;
using FridgeChef.Api.Http;
using FridgeChef.Shared;
using FridgeChef.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Api.Features.ShoppingList;

public sealed class ShoppingListFunction(IShoppingListAgent agent, IModelSelector models, ILogger<ShoppingListFunction> logger)
{
    [Function("shopping-list")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "shopping-list")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var (request, problem) = await JsonBody.ReadAsync<ShoppingListRequest>(req, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        var title = request!.RecipeTitle?.Trim() ?? "";
        var missing = TextList.Clean(request.Missing, ApiLimits.MaxListItems);
        if (title.Length == 0 || missing.Count == 0)
        {
            return Problems.Create(StatusCodes.Status400BadRequest, "Send the recipe title and at least one missing item.");
        }

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
            var result = await agent.PlanAsync(
                title.Length > ApiLimits.MaxTextLength ? title[..ApiLimits.MaxTextLength] : title,
                missing,
                TextList.Clean(request.AlreadyOnList, ApiLimits.MaxIngredients),
                model,
                cancellationToken);
            logger.LogInformation(
                "Shopping agent added {Count} items for {Missing} missing with {Model} ({PromptVersion}), {InputTokens} in / {OutputTokens} out tokens",
                result.Items.Count, missing.Count, model, result.Run.PromptVersion, result.Run.InputTokens, result.Run.OutputTokens);
            return new OkObjectResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Shopping list agent failed with {Model}", model);
            return Problems.Create(StatusCodes.Status502BadGateway, "The AI service couldn't build the shopping list. Try again shortly.");
        }
    }
}

using FridgeChef.Api.Foundry;
using FridgeChef.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FridgeChef.Api.Features.Ingredients;

public sealed class IngredientsFunction(
    IIngredientDetector detector,
    IModelSelector models,
    ILogger<IngredientsFunction> logger)
{
    // Room for the image plus multipart framing and the small "model" field.
    internal const long MaxRequestBytes = ApiLimits.MaxImageBytes + 64 * 1024;

    // TODO(phase 5): require a valid token for the allow-listed account before anything below runs.
    [Function("ingredients")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingredients")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (req.ContentLength > MaxRequestBytes)
        {
            return Problem(StatusCodes.Status413PayloadTooLarge, "The photo is too large.");
        }

        if (!req.HasFormContentType)
        {
            return Problem(StatusCodes.Status400BadRequest, "Send the photo as multipart/form-data.");
        }

        var form = await req.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = MaxRequestBytes }, cancellationToken);

        string model;
        try
        {
            model = models.Resolve(AiTask.Ingredients, form["model"].FirstOrDefault());
        }
        catch (ModelNotAllowedException ex)
        {
            return Problem(StatusCodes.Status400BadRequest, ex.Message);
        }

        var (image, error) = await ImageValidator.ValidateAsync(form.Files.GetFile("image"), cancellationToken);
        if (image is null)
        {
            return Problem(StatusCodes.Status400BadRequest, error!);
        }

        try
        {
            var result = await detector.DetectAsync(image, model, cancellationToken);
            logger.LogInformation(
                "Detected {Count} ingredients with {Model} ({PromptVersion}), {InputTokens} in / {OutputTokens} out tokens",
                result.Ingredients.Count, model, result.Run.PromptVersion, result.Run.InputTokens, result.Run.OutputTokens);
            return new OkObjectResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ingredient detection failed with {Model}", model);
            return Problem(StatusCodes.Status502BadGateway, "The AI service couldn't analyse the photo. Try again shortly.");
        }
    }

    private static ObjectResult Problem(int status, string detail) =>
        new(new ProblemDetails { Status = status, Detail = detail }) { StatusCode = status };
}

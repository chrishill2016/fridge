using System.ComponentModel;
using FridgeChef.Api.Foundry;
using FridgeChef.Shared.Models;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Features.Ingredients;

public interface IIngredientDetector
{
    Task<IngredientsResponse> DetectAsync(ValidatedImage image, string model, CancellationToken cancellationToken);
}

public sealed class IngredientDetector(IChatClientProvider chatClients) : IIngredientDetector
{
    // Bump the version whenever the prompt changes, so results and evaluations can be compared.
    internal const string PromptVersion = "ingredients-v1";
    internal const int MaxIngredients = 60;

    internal const string Instructions = """
        You identify food ingredients in a photo of a fridge, cupboard or worktop.
        List each distinct ingredient you can see that could be used for cooking.
        - Use short, common UK names in lower case (e.g. "cheddar", "spring onions", "chopped tomatoes").
        - Give a rough quantity only when it is clear from the photo (e.g. "6", "half a bottle", "1 tin"); otherwise leave it null.
        - Confidence is 0 to 1: how sure you are the item is what you named.
        - Read labels where you can. Ignore non-food items, packaging on its own and condiments you can't identify.
        - If you can't see any food, return an empty list.
        """;

    public async Task<IngredientsResponse> DetectAsync(ValidatedImage image, string model, CancellationToken cancellationToken)
    {
        var client = chatClients.GetChatClient(model);
        ChatMessage[] messages =
        [
            new(ChatRole.System, Instructions),
            new(ChatRole.User,
            [
                new TextContent("List the ingredients in this photo."),
                new DataContent(image.Bytes, image.MediaType),
            ]),
        ];

        var response = await client.GetResponseAsync<DetectedIngredients>(
            messages, new ChatOptions { ModelId = model }, cancellationToken: cancellationToken);

        return new IngredientsResponse(
            Clean(response.Result.Ingredients),
            new AiRunInfo(model, PromptVersion, response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount));
    }

    /// <summary>Tidies model output: trims, drops blanks, merges duplicates, clamps confidence, most confident first.</summary>
    internal static IReadOnlyList<Ingredient> Clean(IEnumerable<DetectedIngredient>? detected) =>
        [.. (detected ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d.Name))
            .Select(d => new Ingredient(
                d.Name.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(d.Quantity) ? null : d.Quantity.Trim(),
                Math.Clamp(double.IsFinite(d.Confidence) ? d.Confidence : 0, 0, 1)))
            .GroupBy(i => i.Name)
            .Select(g => g.MaxBy(i => i.Confidence)!)
            .OrderByDescending(i => i.Confidence)
            .Take(MaxIngredients)];

    // Shapes the JSON schema sent to the model (structured output).
    internal sealed record DetectedIngredients(
        [property: Description("Ingredients visible in the photo")] List<DetectedIngredient> Ingredients);

    internal sealed record DetectedIngredient(
        [property: Description("Short common name, lower case")] string Name,
        [property: Description("Rough amount if clearly visible, otherwise null")] string? Quantity,
        [property: Description("0 to 1")] double Confidence);
}

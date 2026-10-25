using System.ComponentModel;
using System.Text;
using FridgeChef.Api.Foundry;
using FridgeChef.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Features.Recipes;

public interface IRecipeGenerator
{
    Task<RecipesResponse> SuggestAsync(IReadOnlyList<string> ingredients, RecipePreferences preferences, string model, CancellationToken cancellationToken);
}

/// <summary>A code-defined Agent Framework agent that returns three recipes as structured output.</summary>
public sealed class RecipeGenerator(IChatClientProvider chatClients) : IRecipeGenerator
{
    internal const string PromptVersion = "recipes-v1";
    internal const int RecipeCount = 3;
    internal const int QuickMaxMinutes = 30;

    internal const string Instructions = """
        You are a practical home cook in the UK suggesting what to make from what someone already has.
        Suggest exactly 3 different recipes that use mostly the listed ingredients.
        - Assume basic store-cupboard items are available: salt, pepper, cooking oil, water. Don't list them as missing.
        - "missing" lists only what the cook would have to buy, as short shopping names. Keep it short; prefer recipes that need little or nothing extra.
        - "ingredients" lists everything the recipe uses, with UK quantities (g, ml, tbsp) scaled to the servings asked for.
        - Steps are short and numbered by order, no more than 8.
        - Never use anything the cook dislikes, even as an optional extra.
        - Vary the dishes (e.g. not three omelettes).
        """;

    public async Task<RecipesResponse> SuggestAsync(IReadOnlyList<string> ingredients, RecipePreferences preferences, string model, CancellationToken cancellationToken)
    {
        var agent = new ChatClientAgent(chatClients.GetChatClient(model), new ChatClientAgentOptions
        {
            Name = "recipe-suggester",
            Description = "Suggests three recipes from the ingredients a cook has.",
            ChatOptions = new ChatOptions { ModelId = model, Instructions = Instructions },
        });

        var response = await agent.RunAsync<RecipeSuggestions>(BuildPrompt(ingredients, preferences), cancellationToken: cancellationToken);

        return new RecipesResponse(
            Clean(response.Result.Recipes, preferences),
            new AiRunInfo(model, PromptVersion, response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount));
    }

    internal static string BuildPrompt(IReadOnlyList<string> ingredients, RecipePreferences preferences)
    {
        var prompt = new StringBuilder()
            .AppendLine("Ingredients I have:")
            .AppendJoin('\n', ingredients.Select(i => "- " + i)).AppendLine()
            .AppendLine($"Servings: {preferences.Servings}");
        if (preferences.Dislikes.Count > 0)
        {
            prompt.AppendLine($"I dislike: {string.Join(", ", preferences.Dislikes)}");
        }

        if (preferences.QuickWeeknight)
        {
            prompt.AppendLine($"It's a weeknight: each recipe must take {QuickMaxMinutes} minutes or less in total.");
        }

        return prompt.ToString();
    }

    /// <summary>Keeps the model's output within what the app can show: three recipes, sane times, no blanks.</summary>
    internal static IReadOnlyList<Recipe> Clean(IEnumerable<SuggestedRecipe>? suggested, RecipePreferences preferences) =>
        [.. (suggested ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.Title) && r.Steps is { Count: > 0 })
            .Select(r => new Recipe(
                r.Title.Trim(),
                Math.Clamp(r.TimeMinutes, 1, preferences.QuickWeeknight ? QuickMaxMinutes : 600),
                Tidy(r.Ingredients),
                Tidy(r.Missing),
                Tidy(r.Steps)))
            .DistinctBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .Take(RecipeCount)];

    private static IReadOnlyList<string> Tidy(IEnumerable<string>? items) =>
        [.. (items ?? []).Select(i => i.Trim()).Where(i => i.Length > 0)];

    internal sealed record RecipeSuggestions(
        [property: Description("Exactly 3 recipes")] List<SuggestedRecipe> Recipes);

    internal sealed record SuggestedRecipe(
        string Title,
        [property: Description("Total time including prep, in minutes")] int TimeMinutes,
        [property: Description("Everything used, with quantities")] List<string> Ingredients,
        [property: Description("Only what must be bought; empty if nothing")] List<string> Missing,
        [property: Description("Short method steps in order")] List<string> Steps);
}

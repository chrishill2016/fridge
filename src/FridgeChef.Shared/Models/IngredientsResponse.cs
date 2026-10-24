namespace FridgeChef.Shared.Models;

public sealed record IngredientsResponse(IReadOnlyList<Ingredient> Ingredients, AiRunInfo Run);

/// <summary>Which model and prompt produced a result, and what it cost in tokens.</summary>
public sealed record AiRunInfo(string Model, string PromptVersion, long? InputTokens, long? OutputTokens);

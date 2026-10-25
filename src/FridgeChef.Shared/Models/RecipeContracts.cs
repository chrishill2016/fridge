namespace FridgeChef.Shared.Models;

public sealed record RecipesRequest(IReadOnlyList<string> Ingredients, RecipePreferences Preferences, string? Model = null);

public sealed record RecipesResponse(IReadOnlyList<Recipe> Recipes, AiRunInfo Run);

public sealed record ShoppingListRequest(
    string RecipeTitle,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> AlreadyOnList,
    string? Model = null);

public sealed record ShoppingSuggestion(string Name, string? Quantity = null);

public sealed record ShoppingListResponse(IReadOnlyList<ShoppingSuggestion> Items, AiRunInfo Run);

/// <summary>Deployments the app may choose from, and the defaults used when it doesn't choose.</summary>
public sealed record ModelsResponse(IReadOnlyList<string> Models, string IngredientsDefault, string RecipesDefault);

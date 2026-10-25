using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

/// <summary>Stands in for the API until one is deployed, so the app can be tried end to end.</summary>
public sealed class DemoFridgeChefApi(TimeProvider time) : IFridgeChefApi
{
    internal const string DemoModel = "demo";
    internal static readonly TimeSpan Delay = TimeSpan.FromSeconds(1.5);
    private static readonly AiRunInfo Run = new(DemoModel, DemoModel, null, null);

    public bool IsDemo => true;

    public async Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, time, cancellationToken);
        return new IngredientsResponse(
            [
                new("eggs", "6", 0.97),
                new("cheddar", "half a block", 0.92),
                new("milk", "1 bottle", 0.9),
                new("spring onions", null, 0.81),
                new("cherry tomatoes", "1 punnet", 0.78),
                new("butter", null, 0.74),
                new("greek yoghurt", null, 0.55),
            ],
            Run);
    }

    public async Task<RecipesResponse> SuggestRecipesAsync(RecipesRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, time, cancellationToken);
        var have = request.Ingredients.Take(4).ToList();
        var servings = request.Preferences.Servings;
        return new RecipesResponse(
            [
                new($"Sample: {Cap(have.FirstOrDefault() ?? "store-cupboard")} omelette", 15,
                    [.. have.Select(i => $"{i} (as needed)"), $"serves {servings}"], [],
                    ["This is demo data.", "Whisk, cook gently, fold and serve."]),
                new("Sample: quick tomato pasta", request.Preferences.QuickWeeknight ? 20 : 25,
                    ["300g pasta", "cherry tomatoes", "garlic"], ["pasta", "garlic"],
                    ["This is demo data.", "Boil pasta.", "Blister tomatoes with garlic.", "Toss together."]),
                new("Sample: creamy spinach bake", request.Preferences.QuickWeeknight ? 30 : 40,
                    ["eggs", "cheddar", "spinach", "double cream"], ["spinach", "double cream"],
                    ["This is demo data.", "Layer everything in a dish.", "Bake until golden."]),
            ],
            Run);
    }

    public async Task<ShoppingListResponse> PlanShoppingAsync(ShoppingListRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay / 2, time, cancellationToken);
        return new ShoppingListResponse(
            [.. request.Missing
                .Where(m => !request.AlreadyOnList.Contains(m, StringComparer.OrdinalIgnoreCase))
                .Select(m => new ShoppingSuggestion(m))],
            Run);
    }

    public Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ModelsResponse([DemoModel], DemoModel, DemoModel));

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}

using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;

namespace FridgeChef.Tests.Fakes;

public sealed class FakeApi : IFridgeChefApi
{
    public bool IsDemo { get; set; }
    public Exception? Throw { get; set; }
    public PreparedPhoto? LastPhoto { get; private set; }
    public string? LastPhotoModel { get; private set; }
    public RecipesRequest? LastRecipesRequest { get; private set; }
    public ShoppingListRequest? LastShoppingRequest { get; private set; }
    public int RecipeCalls { get; private set; }

    public IngredientsResponse Response { get; set; } = new(
        [new Ingredient("eggs", "6", 0.95), new Ingredient("yoghurt", null, 0.4)],
        new AiRunInfo("gpt-4.1-mini", "ingredients-v1", 1000, 50));

    public RecipesResponse Recipes { get; set; } = new(
        [
            new Recipe("Omelette", 10, ["3 eggs"], [], ["Whisk", "Cook"]),
            new Recipe("Pasta bake", 40, ["pasta", "cheddar"], ["pasta", "cream"], ["Boil", "Bake"]),
        ],
        new AiRunInfo("gpt-4.1", "recipes-v1", 900, 300));

    /// <summary>Null means "echo the missing items back".</summary>
    public IReadOnlyList<ShoppingSuggestion>? ShoppingItems { get; set; }

    public ModelsResponse Models { get; set; } = new(["gpt-4.1-mini", "gpt-4.1", "phi-4"], "gpt-4.1-mini", "gpt-4.1");

    public Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        (LastPhoto, LastPhotoModel) = (photo, model);
        return Result(Response);
    }

    public Task<RecipesResponse> SuggestRecipesAsync(RecipesRequest request, CancellationToken cancellationToken = default)
    {
        LastRecipesRequest = request;
        RecipeCalls++;
        return Result(Recipes);
    }

    public Task<ShoppingListResponse> PlanShoppingAsync(ShoppingListRequest request, CancellationToken cancellationToken = default)
    {
        LastShoppingRequest = request;
        var items = ShoppingItems ?? [.. request.Missing.Select(m => new ShoppingSuggestion(m))];
        return Result(new ShoppingListResponse(items, new AiRunInfo("gpt-4.1", "shopping-v1", 10, 5)));
    }

    public Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default) => Result(Models);

    private Task<T> Result<T>(T value) => Throw is null ? Task.FromResult(value) : Task.FromException<T>(Throw);
}

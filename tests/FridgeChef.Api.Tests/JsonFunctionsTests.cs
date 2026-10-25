using FridgeChef.Api.Features.Models;
using FridgeChef.Api.Features.Recipes;
using FridgeChef.Api.Features.ShoppingList;
using FridgeChef.Api.Foundry;
using FridgeChef.Api.Tests.Fakes;
using FridgeChef.Shared;
using FridgeChef.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FridgeChef.Api.Tests;

public class JsonFunctionsTests
{
    private readonly ModelSelector _models = new(TestOptions.Foundry("phi-4"));
    private readonly RecordingRecipes _recipes = new();
    private readonly RecordingShopping _shopping = new();

    private RecipesFunction Recipes => new(_recipes, _models, NullLogger<RecipesFunction>.Instance);
    private ShoppingListFunction Shopping => new(_shopping, _models, NullLogger<ShoppingListFunction>.Instance);

    [Fact]
    public async Task Recipes_cleans_input_and_uses_recipes_default_model()
    {
        var request = new RecipesRequest([" eggs ", "Eggs", "", "cheddar"], new RecipePreferences([" olives ", "OLIVES"], true, 50));

        var result = await Recipes.Run(JsonRequests.Post(request), TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(["eggs", "cheddar"], _recipes.Ingredients);
        Assert.Equal(["olives"], _recipes.Preferences!.Dislikes);
        Assert.Equal(RecipePreferences.MaxServings, _recipes.Preferences.Servings);
        Assert.True(_recipes.Preferences.QuickWeeknight);
        Assert.Equal("gpt-4.1", _recipes.Model);
    }

    [Fact]
    public async Task Recipes_without_preferences_uses_defaults()
    {
        await Recipes.Run(JsonRequests.Post("""{"ingredients":["eggs"]}"""), TestContext.Current.CancellationToken);

        Assert.Equal(RecipePreferences.DefaultServings, _recipes.Preferences!.Servings);
    }

    [Theory]
    [InlineData("""{"ingredients":[]}""", 400)]
    [InlineData("""{"ingredients":["  "]}""", 400)]
    [InlineData("""{"ingredients":["eggs"],"model":"gpt-5-pro"}""", 400)]
    [InlineData("not json", 400)]
    [InlineData("null", 400)]
    public async Task Recipes_rejects_bad_requests(string body, int status)
    {
        AssertProblem(await Recipes.Run(JsonRequests.Post(body), TestContext.Current.CancellationToken), status);
        Assert.Null(_recipes.Model);
    }

    [Fact]
    public async Task Recipes_rejects_wrong_content_type_and_oversized_body()
    {
        AssertProblem(await Recipes.Run(JsonRequests.Post("""{"ingredients":["eggs"]}""", "text/plain"), TestContext.Current.CancellationToken), 400);

        var huge = JsonRequests.Post($$"""{"ingredients":["{{new string('a', ApiLimits.MaxJsonBytes)}}"]}""");
        AssertProblem(await Recipes.Run(huge, TestContext.Current.CancellationToken), 413);
    }

    [Fact]
    public async Task Recipes_failure_or_empty_result_is_502()
    {
        _recipes.Throw = new InvalidOperationException("boom");
        AssertProblem(await Recipes.Run(JsonRequests.Post("""{"ingredients":["eggs"]}"""), TestContext.Current.CancellationToken), 502);

        _recipes.Throw = null;
        _recipes.Empty = true;
        AssertProblem(await Recipes.Run(JsonRequests.Post("""{"ingredients":["eggs"]}"""), TestContext.Current.CancellationToken), 502);
    }

    [Fact]
    public async Task Shopping_list_passes_clean_input_and_requested_model()
    {
        var request = new ShoppingListRequest(" Pasta bake ", ["cream", " cream ", "spinach"], ["milk"], "phi-4");

        var result = await Shopping.Run(JsonRequests.Post(request), TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ShoppingListResponse>(ok.Value);
        Assert.Equal("Pasta bake", _shopping.Title);
        Assert.Equal(["cream", "spinach"], _shopping.Missing);
        Assert.Equal(["milk"], _shopping.AlreadyOnList);
        Assert.Equal("phi-4", _shopping.Model);
    }

    [Theory]
    [InlineData("""{"recipeTitle":"","missing":["x"],"alreadyOnList":[]}""")]
    [InlineData("""{"recipeTitle":"Pasta","missing":[],"alreadyOnList":[]}""")]
    [InlineData("""{"recipeTitle":"Pasta","missing":["x"],"alreadyOnList":[],"model":"nope"}""")]
    public async Task Shopping_list_rejects_bad_requests(string body)
    {
        AssertProblem(await Shopping.Run(JsonRequests.Post(body), TestContext.Current.CancellationToken), 400);
        Assert.Null(_shopping.Model);
    }

    [Fact]
    public void Models_lists_allowed_and_defaults()
    {
        var sut = new ModelsFunction(_models, TestOptions.Foundry("phi-4"));

        var ok = Assert.IsType<OkObjectResult>(sut.Run(new DefaultHttpContext().Request));
        var body = Assert.IsType<ModelsResponse>(ok.Value);
        Assert.Equal(["gpt-4.1-mini", "gpt-4.1", "phi-4"], body.Models);
        Assert.Equal("gpt-4.1-mini", body.IngredientsDefault);
        Assert.Equal("gpt-4.1", body.RecipesDefault);
    }

    private static void AssertProblem(IActionResult result, int status)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    private sealed class RecordingRecipes : IRecipeGenerator
    {
        public IReadOnlyList<string>? Ingredients { get; private set; }
        public RecipePreferences? Preferences { get; private set; }
        public string? Model { get; private set; }
        public Exception? Throw { get; set; }
        public bool Empty { get; set; }

        public Task<RecipesResponse> SuggestAsync(IReadOnlyList<string> ingredients, RecipePreferences preferences, string model, CancellationToken cancellationToken)
        {
            (Ingredients, Preferences, Model) = (ingredients, preferences, model);
            if (Throw is not null)
            {
                throw Throw;
            }

            IReadOnlyList<Recipe> recipes = Empty ? [] : [new Recipe("Omelette", 10, ["eggs"], [], ["Cook"])];
            return Task.FromResult(new RecipesResponse(recipes, new AiRunInfo(model, "test", 1, 1)));
        }
    }

    private sealed class RecordingShopping : IShoppingListAgent
    {
        public string? Title { get; private set; }
        public IReadOnlyList<string>? Missing { get; private set; }
        public IReadOnlyList<string>? AlreadyOnList { get; private set; }
        public string? Model { get; private set; }

        public Task<ShoppingListResponse> PlanAsync(string recipeTitle, IReadOnlyList<string> missing, IReadOnlyList<string> alreadyOnList, string model, CancellationToken cancellationToken)
        {
            (Title, Missing, AlreadyOnList, Model) = (recipeTitle, missing, alreadyOnList, model);
            return Task.FromResult(new ShoppingListResponse([new("cream")], new AiRunInfo(model, "test", 1, 1)));
        }
    }
}

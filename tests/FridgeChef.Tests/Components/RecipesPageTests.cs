using FridgeChef.Features.Recipes;
using FridgeChef.Models;
using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Components;

public class RecipesPageTests : AppTestContext
{
    private ISettingsService Settings => Services.GetRequiredService<ISettingsService>();
    private ISavedRecipeService Saved => Services.GetRequiredService<ISavedRecipeService>();
    private IShoppingListService ShoppingList => Services.GetRequiredService<IShoppingListService>();

    [Fact]
    public void Without_ingredients_points_to_scan_and_calls_nothing()
    {
        var cut = Render<RecipesPage>();

        cut.Find("[data-testid=no-ingredients]");
        Assert.Equal(0, Api.RecipeCalls);
    }

    [Fact]
    public async Task Arriving_with_ingredients_suggests_using_preferences_and_model()
    {
        await Settings.SavePreferencesAsync(new RecipePreferences(["olives"], true, 4));
        await Settings.SaveAppSettingsAsync(new AppSettings(Model: "phi-4"));
        Scanned("eggs", "cheddar");

        var cut = Render<RecipesPage>();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=recipe-card]").Count));
        Assert.Equal(["eggs", "cheddar"], Api.LastRecipesRequest!.Ingredients);
        Assert.Equal(["olives"], Api.LastRecipesRequest.Preferences.Dislikes);
        Assert.True(Api.LastRecipesRequest.Preferences.QuickWeeknight);
        Assert.Equal("phi-4", Api.LastRecipesRequest.Model);
        Assert.Contains("4 servings · quick weeknight · no olives", cut.Find("[data-testid=preferences]").TextContent);
        Assert.Contains("gpt-4.1 · recipes-v1 · 1,200 tokens", cut.Find("[data-testid=run-info]").TextContent);
    }

    [Fact]
    public void Recipes_are_kept_when_returning_and_not_requested_again()
    {
        Scanned("eggs");
        var first = Render<RecipesPage>();
        first.WaitForElement("[data-testid=recipe-card]");
        first.Dispose();

        var second = Render<RecipesPage>();

        Assert.Equal(2, second.FindAll("[data-testid=recipe-card]").Count);
        Assert.Equal(1, Api.RecipeCalls);
    }

    [Fact]
    public async Task Heart_saves_and_unsaves_a_recipe()
    {
        Scanned("eggs");
        var cut = Render<RecipesPage>();
        cut.WaitForElement("[data-testid=recipe-card]");

        cut.FindAll("[data-testid=toggle-saved]")[0].Click();
        cut.WaitForAssertion(async () => Assert.True(await Saved.IsSavedAsync("Omelette")));
        Assert.Contains("Remove from favourites", cut.FindAll("[data-testid=toggle-saved]")[0].GetAttribute("aria-label"));

        cut.FindAll("[data-testid=toggle-saved]")[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("Save to favourites", cut.FindAll("[data-testid=toggle-saved]")[0].GetAttribute("aria-label")));
        Assert.False(await Saved.IsSavedAsync("Omelette"));
    }

    [Fact]
    public async Task Add_missing_goes_through_the_shopping_agent()
    {
        Api.ShoppingItems = [new("penne", "500g"), new("double cream", "300ml")];
        Scanned("eggs");
        var cut = Render<RecipesPage>();

        cut.WaitForElement("[data-testid=add-missing]").Click();

        cut.WaitForAssertion(() => Assert.Equal("Pasta bake", Api.LastShoppingRequest?.RecipeTitle));
        Assert.Equal(["penne", "double cream"], (await ShoppingList.GetItemsAsync()).Select(i => i.Name));
    }

    [Fact]
    public void Error_is_shown_and_retry_works()
    {
        Api.Throw = new ApiException("The AI service couldn't suggest recipes.");
        Scanned("eggs");
        var cut = Render<RecipesPage>();
        cut.WaitForAssertion(() => Assert.Contains("couldn't suggest", cut.Find("[data-testid=error]").TextContent));

        Api.Throw = null;
        cut.Find("[data-testid=suggest]").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=recipe-card]").Count));
        Assert.Empty(cut.FindAll("[data-testid=error]"));
    }

    [Fact]
    public void Offline_does_not_call_and_disables_suggest()
    {
        Connectivity.Set(false);
        Scanned("eggs");

        var cut = Render<RecipesPage>();

        cut.Find("[data-testid=offline-warning]");
        Assert.True(cut.Find("[data-testid=suggest]").HasAttribute("disabled"));
        Assert.Equal(0, Api.RecipeCalls);
    }

    private void Scanned(params string[] names) =>
        Session.SetResult(new IngredientsResponse([.. names.Select(n => new Ingredient(n))], new AiRunInfo("m", "v", 1, 1)));
}

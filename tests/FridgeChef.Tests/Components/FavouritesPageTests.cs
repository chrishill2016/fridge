using FridgeChef.Features.Recipes;
using FridgeChef.Services;
using FridgeChef.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Components;

public class FavouritesPageTests : AppTestContext
{
    private ISavedRecipeService Saved => Services.GetRequiredService<ISavedRecipeService>();
    private IShoppingListService ShoppingList => Services.GetRequiredService<IShoppingListService>();

    [Fact]
    public void Shows_empty_state()
    {
        var cut = Render<FavouritesPage>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=empty]"));
    }

    [Fact]
    public async Task Lists_saved_recipes_and_removes_on_unfavourite()
    {
        await Saved.SaveAsync(TestData.Recipe("Omelette"));
        var cut = Render<FavouritesPage>();
        cut.WaitForAssertion(() => Assert.Contains("Omelette", cut.Find("[data-testid=recipe-card]").TextContent));

        cut.Find("[data-testid=toggle-saved]").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid=empty]"));
        Assert.Empty(await Saved.GetAllAsync());
    }

    [Fact]
    public async Task Add_missing_puts_items_on_shopping_list()
    {
        await Saved.SaveAsync(TestData.Recipe("Omelette", "chives", "butter"));
        var cut = Render<FavouritesPage>();

        cut.WaitForElement("[data-testid=add-missing]").Click();

        Assert.Equal(["chives", "butter"], (await ShoppingList.GetItemsAsync()).Select(i => i.Name));
    }
}

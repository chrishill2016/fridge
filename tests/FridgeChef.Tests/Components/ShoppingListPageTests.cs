using FridgeChef.Features.ShoppingList;
using FridgeChef.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Components;

public class ShoppingListPageTests : AppTestContext
{
    private IShoppingListService ShoppingList => Services.GetRequiredService<IShoppingListService>();

    [Fact]
    public void Shows_empty_state()
    {
        var cut = Render<ShoppingListPage>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=empty]"));
    }

    [Fact]
    public void Adding_an_item_lists_it_and_clears_input()
    {
        var cut = Render<ShoppingListPage>();

        cut.Find("input[data-testid=new-item]").Input("Milk");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Milk", cut.Find("[data-testid=shopping-item]").TextContent));
        Assert.Equal("", cut.Find("input[data-testid=new-item]").GetAttribute("value") ?? "");
    }

    [Fact]
    public async Task Marking_bought_then_clearing_removes_item()
    {
        await ShoppingList.AddAsync("Milk");
        await ShoppingList.AddAsync("Eggs");
        var cut = Render<ShoppingListPage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=shopping-item]").Count));

        cut.FindAll("[data-testid=shopping-item] input[type=checkbox]")[0].Change(true);
        cut.WaitForElement("[data-testid=clear-bought]").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid=shopping-item]")));
        Assert.Equal(["Eggs"], (await ShoppingList.GetItemsAsync()).Select(i => i.Name));
    }

    [Fact]
    public async Task Shows_which_recipe_an_item_is_for()
    {
        await ShoppingList.AddAsync("Chives", "Omelette");

        var cut = Render<ShoppingListPage>();

        cut.WaitForAssertion(() => Assert.Contains("for Omelette", cut.Find("[data-testid=shopping-item]").TextContent));
    }
}

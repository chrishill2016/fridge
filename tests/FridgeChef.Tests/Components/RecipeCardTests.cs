using FridgeChef.Features.Recipes;
using FridgeChef.Tests.Fakes;

namespace FridgeChef.Tests.Components;

public class RecipeCardTests : AppTestContext
{
    [Fact]
    public void Renders_recipe_details()
    {
        var cut = Render<RecipeCard>(p => p.Add(c => c.Recipe, TestData.Recipe("Omelette", "chives")));

        Assert.Contains("Omelette", cut.Markup);
        Assert.Contains("15 min", cut.Markup);
        Assert.Contains("50g cheddar", cut.Markup);
        Assert.Contains("chives", cut.Find("[data-testid=missing]").TextContent);
        Assert.Contains("Whisk the eggs.", cut.Markup);
    }

    [Fact]
    public void Hides_missing_section_and_button_when_nothing_missing()
    {
        var cut = Render<RecipeCard>(p => p
            .Add(c => c.Recipe, TestData.Recipe())
            .Add(c => c.OnAddMissing, () => { }));

        Assert.Empty(cut.FindAll("[data-testid=missing]"));
        Assert.Empty(cut.FindAll("[data-testid=add-missing]"));
    }

    [Fact]
    public void Heart_raises_toggle_saved()
    {
        var toggled = false;
        var cut = Render<RecipeCard>(p => p
            .Add(c => c.Recipe, TestData.Recipe())
            .Add(c => c.OnToggleSaved, () => toggled = true));

        cut.Find("[data-testid=toggle-saved]").Click();

        Assert.True(toggled);
    }
}

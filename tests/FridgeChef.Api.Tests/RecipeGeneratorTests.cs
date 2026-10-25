using FridgeChef.Api.Features.Recipes;
using FridgeChef.Api.Tests.Fakes;
using FridgeChef.Shared.Models;
using static FridgeChef.Api.Features.Recipes.RecipeGenerator;

namespace FridgeChef.Api.Tests;

public class RecipeGeneratorTests
{
    private const string ModelJson = """
        {"recipes":[
          {"title":"Cheese omelette","timeMinutes":10,"ingredients":["3 eggs","40g cheddar"],"missing":[],"steps":["Whisk","Cook"]},
          {"title":" Frittata ","timeMinutes":45,"ingredients":["6 eggs"],"missing":["spinach"," "],"steps":["Bake"]},
          {"title":"cheese omelette","timeMinutes":12,"ingredients":[],"missing":[],"steps":["Duplicate"]},
          {"title":"No steps","timeMinutes":5,"ingredients":[],"missing":[],"steps":[]},
          {"title":"Shakshuka","timeMinutes":25,"ingredients":["4 eggs"],"missing":["peppers"],"steps":["Simmer","Crack eggs in"]},
          {"title":"Fourth","timeMinutes":5,"ingredients":[],"missing":[],"steps":["x"]}
        ]}
        """;

    [Fact]
    public async Task Agent_gets_instructions_model_and_prompt_and_result_is_cleaned()
    {
        var chat = new ScriptedChatClient(ScriptedChatClient.Text(ModelJson, 900, 400));
        var provider = new StubProvider(chat);
        var sut = new RecipeGenerator(provider);
        var preferences = new RecipePreferences(["mushrooms"], QuickWeeknight: true, Servings: 4);

        var result = await sut.SuggestAsync(["eggs", "cheddar"], preferences, "gpt-4.1", TestContext.Current.CancellationToken);

        Assert.Equal(["gpt-4.1"], provider.RequestedModels);
        var (messages, options) = Assert.Single(chat.Calls);
        Assert.Equal("gpt-4.1", options?.ModelId);
        Assert.NotNull(options?.ResponseFormat);
        var allText = string.Join("\n", messages.Select(m => m.Text)) + options?.Instructions;
        Assert.Contains("Suggest exactly 3", allText);
        Assert.Contains("- eggs", allText);
        Assert.Contains("I dislike: mushrooms", allText);
        Assert.Contains("Servings: 4", allText);
        Assert.Contains("30 minutes or less", allText);

        Assert.Equal(["Cheese omelette", "Frittata", "Shakshuka"], result.Recipes.Select(r => r.Title));
        Assert.Equal(QuickMaxMinutes, result.Recipes[1].TimeMinutes);
        Assert.Equal(["spinach"], result.Recipes[1].Missing);
        Assert.Equal(new AiRunInfo("gpt-4.1", PromptVersion, 900, 400), result.Run);
    }

    [Fact]
    public void Prompt_leaves_out_optional_preferences()
    {
        var prompt = BuildPrompt(["eggs"], RecipePreferences.Default);

        Assert.DoesNotContain("dislike", prompt);
        Assert.DoesNotContain("weeknight", prompt);
        Assert.Contains($"Servings: {RecipePreferences.DefaultServings}", prompt);
    }

    [Fact]
    public void Clean_allows_long_recipes_when_not_quick() =>
        Assert.Equal(90, Assert.Single(Clean([new SuggestedRecipe("Stew", 90, [], [], ["Simmer"])], RecipePreferences.Default)).TimeMinutes);
}

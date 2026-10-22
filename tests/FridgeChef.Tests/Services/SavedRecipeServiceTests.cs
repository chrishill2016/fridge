using FridgeChef.Services;
using FridgeChef.Tests.Fakes;

namespace FridgeChef.Tests.Services;

public class SavedRecipeServiceTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly SavedRecipeService _sut;

    public SavedRecipeServiceTests() => _sut = new SavedRecipeService(new InMemoryLocalStore(), _time);

    [Fact]
    public async Task Save_round_trips_full_recipe()
    {
        var recipe = TestData.Recipe("Omelette", "chives");

        var saved = await _sut.SaveAsync(recipe);
        var loaded = Assert.Single(await _sut.GetAllAsync());

        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal(_time.GetUtcNow(), loaded.SavedUtc);
        Assert.Equal(recipe.Title, loaded.Recipe.Title);
        Assert.Equal(recipe.TimeMinutes, loaded.Recipe.TimeMinutes);
        Assert.Equal(recipe.Ingredients, loaded.Recipe.Ingredients);
        Assert.Equal(recipe.Missing, loaded.Recipe.Missing);
        Assert.Equal(recipe.Steps, loaded.Recipe.Steps);
    }

    [Fact]
    public async Task Save_replaces_recipe_with_same_title()
    {
        var first = await _sut.SaveAsync(TestData.Recipe("Omelette"));
        _time.Advance(TimeSpan.FromDays(1));

        var second = await _sut.SaveAsync(TestData.Recipe(" omelette ", "chives"));

        Assert.Equal(first.Id, second.Id);
        var only = Assert.Single(await _sut.GetAllAsync());
        Assert.Equal(["chives"], only.Recipe.Missing);
    }

    [Fact]
    public async Task GetAll_returns_newest_first()
    {
        await _sut.SaveAsync(TestData.Recipe("Old"));
        _time.Advance(TimeSpan.FromMinutes(5));
        await _sut.SaveAsync(TestData.Recipe("New"));

        Assert.Equal(["New", "Old"], (await _sut.GetAllAsync()).Select(r => r.Recipe.Title));
    }

    [Fact]
    public async Task IsSaved_matches_title_ignoring_case()
    {
        await _sut.SaveAsync(TestData.Recipe("Omelette"));

        Assert.True(await _sut.IsSavedAsync("OMELETTE"));
        Assert.False(await _sut.IsSavedAsync("Pancakes"));
    }

    [Fact]
    public async Task Remove_deletes_recipe()
    {
        var saved = await _sut.SaveAsync(TestData.Recipe());

        await _sut.RemoveAsync(saved.Id);

        Assert.Empty(await _sut.GetAllAsync());
    }
}

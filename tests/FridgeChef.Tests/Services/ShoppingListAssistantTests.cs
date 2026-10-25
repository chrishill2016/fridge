using FridgeChef.Models;
using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;
using FridgeChef.Tests.Fakes;

namespace FridgeChef.Tests.Services;

public class ShoppingListAssistantTests
{
    private static readonly Recipe Recipe = new("Pasta bake", 40, [], ["cream", "pasta", "spinach"], ["Bake"]);

    private readonly ShoppingListService _list = new(new InMemoryLocalStore(), new ManualTimeProvider());
    private readonly FakeApi _api = new();
    private readonly FakeConnectivityService _connectivity = new();
    private readonly SettingsService _settings = new(new InMemoryLocalStore());
    private readonly ShoppingListAssistant _sut;

    public ShoppingListAssistantTests() => _sut = new ShoppingListAssistant(_list, _api, _connectivity, _settings);

    [Fact]
    public async Task Online_uses_agent_items_with_quantities_and_sends_context()
    {
        await _list.AddAsync("milk");
        var bought = await _list.AddAsync("bread");
        await _list.ToggleBoughtAsync(bought!.Id);
        await _settings.SaveAppSettingsAsync(new AppSettings(Model: "phi-4"));
        _api.ShoppingItems = [new("double cream", "300ml"), new("penne", "500g")];

        var result = await _sut.AddMissingAsync(Recipe, TestContext.Current.CancellationToken);

        Assert.Equal(new AddMissingResult(2, true), result);
        Assert.Equal(["milk"], _api.LastShoppingRequest!.AlreadyOnList);
        Assert.Equal("phi-4", _api.LastShoppingRequest.Model);
        Assert.Equal(Recipe.Missing, _api.LastShoppingRequest.Missing);
        var cream = (await _list.GetItemsAsync()).Single(i => i.Name == "double cream");
        Assert.Equal("300ml", cream.Quantity);
        Assert.Equal("Pasta bake", cream.FromRecipe);
    }

    [Fact]
    public async Task Offline_adds_missing_as_listed_without_calling_api()
    {
        _connectivity.Set(false);

        var result = await _sut.AddMissingAsync(Recipe, TestContext.Current.CancellationToken);

        Assert.Equal(new AddMissingResult(3, false), result);
        Assert.Null(_api.LastShoppingRequest);
    }

    [Fact]
    public async Task Api_failure_falls_back_with_warning()
    {
        _api.Throw = new ApiException("The AI service couldn't build the shopping list.");

        var result = await _sut.AddMissingAsync(Recipe, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Added);
        Assert.False(result.UsedAi);
        Assert.Contains("couldn't build", result.Warning);
    }

    [Fact]
    public async Task Nothing_missing_does_nothing()
    {
        var result = await _sut.AddMissingAsync(Recipe with { Missing = [] }, TestContext.Current.CancellationToken);

        Assert.Equal(new AddMissingResult(0, false), result);
        Assert.Null(_api.LastShoppingRequest);
    }
}

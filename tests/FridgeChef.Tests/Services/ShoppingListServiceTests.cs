using FridgeChef.Services;
using FridgeChef.Services.Storage;
using FridgeChef.Tests.Fakes;

namespace FridgeChef.Tests.Services;

public class ShoppingListServiceTests
{
    private readonly InMemoryLocalStore _store = new();
    private readonly ManualTimeProvider _time = new();
    private readonly ShoppingListService _sut;

    public ShoppingListServiceTests() => _sut = new ShoppingListService(_store, _time);

    [Fact]
    public async Task Add_trims_and_stores_item()
    {
        var item = await _sut.AddAsync("  milk ", "Pancakes");

        Assert.NotNull(item);
        Assert.Equal("milk", item.Name);
        Assert.Equal("Pancakes", item.FromRecipe);
        Assert.False(item.Bought);
        Assert.Equal(_time.GetUtcNow(), item.AddedUtc);
        Assert.Single(await _sut.GetItemsAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_ignores_blank_names(string name)
    {
        Assert.Null(await _sut.AddAsync(name));
        Assert.Empty(await _sut.GetItemsAsync());
    }

    [Fact]
    public async Task Add_does_not_duplicate_unbought_item_ignoring_case()
    {
        var first = await _sut.AddAsync("Milk");
        var second = await _sut.AddAsync("milk");

        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(1, _store.Count(StoreNames.ShoppingList));
    }

    [Fact]
    public async Task Add_puts_bought_item_back_on_the_list()
    {
        var first = await _sut.AddAsync("Milk");
        await _sut.ToggleBoughtAsync(first!.Id);
        _time.Advance(TimeSpan.FromHours(1));

        var again = await _sut.AddAsync("milk", "Pancakes");

        Assert.Equal(first.Id, again!.Id);
        Assert.False(again.Bought);
        Assert.Equal("Pancakes", again.FromRecipe);
        Assert.Equal(_time.GetUtcNow(), again.AddedUtc);
        Assert.Equal(1, _store.Count(StoreNames.ShoppingList));
    }

    [Fact]
    public async Task AddRange_adds_each_distinct_item()
    {
        await _sut.AddRangeAsync(["eggs", "Eggs", "flour", " "], "Pancakes");

        var items = await _sut.GetItemsAsync();
        Assert.Equal(["eggs", "flour"], items.Select(i => i.Name));
        Assert.All(items, i => Assert.Equal("Pancakes", i.FromRecipe));
    }

    [Fact]
    public async Task GetItems_lists_unbought_oldest_first_then_bought()
    {
        var bread = await _sut.AddAsync("bread");
        _time.Advance(TimeSpan.FromMinutes(1));
        await _sut.AddAsync("milk");
        _time.Advance(TimeSpan.FromMinutes(1));
        await _sut.AddAsync("eggs");
        await _sut.ToggleBoughtAsync(bread!.Id);

        var items = await _sut.GetItemsAsync();

        Assert.Equal(["milk", "eggs", "bread"], items.Select(i => i.Name));
    }

    [Fact]
    public async Task ToggleBought_flips_state_and_ignores_unknown_id()
    {
        var item = await _sut.AddAsync("milk");

        await _sut.ToggleBoughtAsync(item!.Id);
        Assert.True((await _sut.GetItemsAsync())[0].Bought);

        await _sut.ToggleBoughtAsync(item.Id);
        Assert.False((await _sut.GetItemsAsync())[0].Bought);

        await _sut.ToggleBoughtAsync("missing");
        Assert.Single(await _sut.GetItemsAsync());
    }

    [Fact]
    public async Task Remove_deletes_item()
    {
        var item = await _sut.AddAsync("milk");

        await _sut.RemoveAsync(item!.Id);

        Assert.Empty(await _sut.GetItemsAsync());
    }

    [Fact]
    public async Task ClearBought_removes_only_bought_items()
    {
        var milk = await _sut.AddAsync("milk");
        await _sut.AddAsync("eggs");
        await _sut.ToggleBoughtAsync(milk!.Id);

        await _sut.ClearBoughtAsync();

        Assert.Equal(["eggs"], (await _sut.GetItemsAsync()).Select(i => i.Name));
    }
}

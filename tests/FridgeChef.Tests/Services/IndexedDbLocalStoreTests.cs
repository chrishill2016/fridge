using FridgeChef.Models;
using FridgeChef.Services.Storage;

namespace FridgeChef.Tests.Services;

public class IndexedDbLocalStoreTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;
    private readonly IndexedDbLocalStore _sut;

    public IndexedDbLocalStoreTests()
    {
        _module = JSInterop.SetupModule(IndexedDbLocalStore.ModulePath);
        _sut = new IndexedDbLocalStore(JSInterop.JSRuntime);
    }

    [Fact]
    public async Task Get_calls_module_with_store_and_key()
    {
        var item = new ShoppingListItem("a", "milk", false, DateTimeOffset.UnixEpoch);
        _module.Setup<ShoppingListItem?>("get", StoreNames.ShoppingList, "a").SetResult(item);

        Assert.Equal(item, await _sut.GetAsync<ShoppingListItem>(StoreNames.ShoppingList, "a"));
    }

    [Fact]
    public async Task GetAll_returns_module_results()
    {
        var items = new[] { new ShoppingListItem("a", "milk", false, DateTimeOffset.UnixEpoch) };
        _module.Setup<ShoppingListItem[]>("getAll", StoreNames.ShoppingList).SetResult(items);

        Assert.Equal(items, await _sut.GetAllAsync<ShoppingListItem>(StoreNames.ShoppingList));
    }

    [Fact]
    public async Task Writes_call_matching_module_functions()
    {
        var item = new ShoppingListItem("a", "milk", false, DateTimeOffset.UnixEpoch);
        _module.SetupVoid("put", StoreNames.ShoppingList, "a", item).SetVoidResult();
        _module.SetupVoid("remove", StoreNames.ShoppingList, "a").SetVoidResult();
        _module.SetupVoid("clear", StoreNames.ShoppingList).SetVoidResult();

        await _sut.PutAsync(StoreNames.ShoppingList, "a", item);
        await _sut.DeleteAsync(StoreNames.ShoppingList, "a");
        await _sut.ClearAsync(StoreNames.ShoppingList);

        _module.VerifyInvoke("put");
        _module.VerifyInvoke("remove");
        _module.VerifyInvoke("clear");
    }

    [Fact]
    public async Task Module_is_imported_once()
    {
        _module.Mode = JSRuntimeMode.Loose;

        await _sut.ClearAsync(StoreNames.Settings);
        await _sut.ClearAsync(StoreNames.SavedRecipes);

        Assert.Single(JSInterop.Invocations, i => i.Identifier == "import");
    }

    [Fact]
    public async Task Unknown_store_is_rejected_before_calling_js()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAllAsync<ShoppingListItem>("nope"));
        Assert.Empty(JSInterop.Invocations);
    }
}

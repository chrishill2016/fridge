using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Services.Browser;
using FridgeChef.Services.Storage;
using FridgeChef.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FridgeChef.Tests.Components;

/// <summary>bUnit context with MudBlazor and the app's services wired to in-memory fakes.</summary>
public abstract class AppTestContext : BunitContext
{
    protected InMemoryLocalStore Store { get; } = new();
    protected ManualTimeProvider Time { get; } = new();
    protected FakeConnectivityService Connectivity { get; } = new();
    protected FakeAppUpdateService Updates { get; } = new();
    protected FakeApi Api { get; } = new();
    protected FakePhotoPreparer PhotoPreparer { get; } = new();
    protected ScanSession Session { get; } = new();

    protected AppTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<TimeProvider>(Time);
        Services.AddSingleton<ILocalStore>(Store);
        Services.AddSingleton<IConnectivityService>(Connectivity);
        Services.AddSingleton<IAppUpdateService>(Updates);
        Services.AddSingleton<IAppInfo>(new StubAppInfo("1.0.42"));
        Services.AddSingleton<ISettingsService, SettingsService>();
        Services.AddSingleton<IShoppingListService, ShoppingListService>();
        Services.AddSingleton<ISavedRecipeService, SavedRecipeService>();
        Services.AddSingleton<IFridgeChefApi>(Api);
        Services.AddSingleton<IPhotoPreparer>(PhotoPreparer);
        Services.AddSingleton(Session);

        // MudBlazor popovers (tooltips, snackbars) need their providers in the render tree.
        Render<MudPopoverProvider>();
        Render<MudSnackbarProvider>();
    }

    private sealed record StubAppInfo(string Version) : IAppInfo;
}

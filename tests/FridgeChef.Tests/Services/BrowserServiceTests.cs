using FridgeChef.Services.Browser;
using Microsoft.JSInterop;

namespace FridgeChef.Tests.Services;

public class BrowserServiceTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public BrowserServiceTests()
    {
        _module = JSInterop.SetupModule(BrowserModules.AppInterop);
        _module.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task Connectivity_reads_initial_state_from_browser()
    {
        _module.Setup<bool>("watchConnectivity", _ => true).SetResult(false);
        var sut = new ConnectivityService(JSInterop.JSRuntime);

        await sut.InitializeAsync();

        Assert.False(sut.IsOnline);
    }

    [Fact]
    public async Task Connectivity_raises_changed_only_on_transition()
    {
        _module.Setup<bool>("watchConnectivity", _ => true).SetResult(true);
        var sut = new ConnectivityService(JSInterop.JSRuntime);
        await sut.InitializeAsync();
        var changes = new List<bool>();
        sut.Changed += changes.Add;

        sut.OnConnectivityChanged(true);
        sut.OnConnectivityChanged(false);
        sut.OnConnectivityChanged(false);
        sut.OnConnectivityChanged(true);

        Assert.Equal([false, true], changes);
        Assert.True(sut.IsOnline);
    }

    [Fact]
    public async Task Connectivity_initialises_once()
    {
        _module.Setup<bool>("watchConnectivity", _ => true).SetResult(true);
        var sut = new ConnectivityService(JSInterop.JSRuntime);

        await sut.InitializeAsync();
        await sut.InitializeAsync();

        Assert.Single(_module.Invocations, i => i.Identifier == "watchConnectivity");
    }

    [Fact]
    public async Task AppUpdate_flags_update_once_and_applies_via_js()
    {
        var sut = new AppUpdateService(JSInterop.JSRuntime);
        await sut.InitializeAsync();
        var raised = 0;
        sut.UpdateAvailableChanged += () => raised++;

        sut.OnUpdateAvailable();
        sut.OnUpdateAvailable();
        await sut.ApplyUpdateAsync();

        Assert.True(sut.UpdateAvailable);
        Assert.Equal(1, raised);
        _module.VerifyInvoke("watchForUpdates");
        _module.VerifyInvoke("applyUpdate");
    }

    [Fact]
    public void AppUpdate_methods_are_js_invokable()
    {
        Assert.NotNull(typeof(AppUpdateService).GetMethod(nameof(AppUpdateService.OnUpdateAvailable))!
            .GetCustomAttributes(typeof(JSInvokableAttribute), false).SingleOrDefault());
        Assert.NotNull(typeof(ConnectivityService).GetMethod(nameof(ConnectivityService.OnConnectivityChanged))!
            .GetCustomAttributes(typeof(JSInvokableAttribute), false).SingleOrDefault());
    }
}

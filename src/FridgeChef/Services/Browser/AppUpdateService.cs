using Microsoft.JSInterop;

namespace FridgeChef.Services.Browser;

public sealed class AppUpdateService(IJSRuntime js) : IAppUpdateService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<AppUpdateService>? _selfRef;

    public bool UpdateAvailable { get; private set; }

    public event Action? UpdateAvailableChanged;

    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", BrowserModules.AppInterop);
        _selfRef = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("watchForUpdates", _selfRef);
    }

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        if (UpdateAvailable)
        {
            return;
        }

        UpdateAvailable = true;
        UpdateAvailableChanged?.Invoke();
    }

    public async Task ApplyUpdateAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("applyUpdate");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfRef?.Dispose();
    }
}

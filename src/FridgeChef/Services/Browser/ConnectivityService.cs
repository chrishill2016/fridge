using Microsoft.JSInterop;

namespace FridgeChef.Services.Browser;

public sealed class ConnectivityService(IJSRuntime js) : IConnectivityService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<ConnectivityService>? _selfRef;

    public bool IsOnline { get; private set; } = true;

    public event Action<bool>? Changed;

    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", BrowserModules.AppInterop);
        _selfRef = DotNetObjectReference.Create(this);
        IsOnline = await _module.InvokeAsync<bool>("watchConnectivity", _selfRef);
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        if (isOnline == IsOnline)
        {
            return;
        }

        IsOnline = isOnline;
        Changed?.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("unwatchConnectivity");
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfRef?.Dispose();
    }
}

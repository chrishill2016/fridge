using Microsoft.JSInterop;

namespace FridgeChef.Services.Storage;

public sealed class IndexedDbLocalStore(IJSRuntime js) : ILocalStore, IAsyncDisposable
{
    internal const string ModulePath = "./js/localStore.js";

    private readonly Lazy<Task<IJSObjectReference>> _module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());

    public async Task<T?> GetAsync<T>(string store, string key)
    {
        StoreNames.EnsureValid(store);
        var module = await _module.Value;
        return await module.InvokeAsync<T?>("get", store, key);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string store)
    {
        StoreNames.EnsureValid(store);
        var module = await _module.Value;
        return await module.InvokeAsync<T[]>("getAll", store);
    }

    public async Task PutAsync<T>(string store, string key, T value)
    {
        StoreNames.EnsureValid(store);
        var module = await _module.Value;
        await module.InvokeVoidAsync("put", store, key, value);
    }

    public async Task DeleteAsync(string store, string key)
    {
        StoreNames.EnsureValid(store);
        var module = await _module.Value;
        await module.InvokeVoidAsync("remove", store, key);
    }

    public async Task ClearAsync(string store)
    {
        StoreNames.EnsureValid(store);
        var module = await _module.Value;
        await module.InvokeVoidAsync("clear", store);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            try
            {
                await (await _module.Value).DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}

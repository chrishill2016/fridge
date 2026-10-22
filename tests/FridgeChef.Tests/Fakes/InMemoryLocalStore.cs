using System.Text.Json;
using FridgeChef.Services.Storage;

namespace FridgeChef.Tests.Fakes;

/// <summary>
/// ILocalStore that round-trips values through JSON, like the IndexedDB interop does,
/// so serialisation problems with the models show up in unit tests.
/// </summary>
public sealed class InMemoryLocalStore : ILocalStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, Dictionary<string, string>> _stores = [];

    public Task<T?> GetAsync<T>(string store, string key)
    {
        StoreNames.EnsureValid(store);
        return Task.FromResult(Store(store).TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, Options) : default);
    }

    public Task<IReadOnlyList<T>> GetAllAsync<T>(string store)
    {
        StoreNames.EnsureValid(store);
        IReadOnlyList<T> all = [.. Store(store).Values.Select(json => JsonSerializer.Deserialize<T>(json, Options)!)];
        return Task.FromResult(all);
    }

    public Task PutAsync<T>(string store, string key, T value)
    {
        StoreNames.EnsureValid(store);
        Store(store)[key] = JsonSerializer.Serialize(value, Options);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string store, string key)
    {
        StoreNames.EnsureValid(store);
        Store(store).Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync(string store)
    {
        StoreNames.EnsureValid(store);
        Store(store).Clear();
        return Task.CompletedTask;
    }

    public int Count(string store) => Store(store).Count;

    private Dictionary<string, string> Store(string name) =>
        _stores.TryGetValue(name, out var store) ? store : _stores[name] = [];
}

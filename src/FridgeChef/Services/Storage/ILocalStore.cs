namespace FridgeChef.Services.Storage;

/// <summary>
/// Key/value persistence on the device. Kept behind an interface so a sync layer
/// (outbox + remote store) can be added later without touching feature code.
/// </summary>
public interface ILocalStore
{
    Task<T?> GetAsync<T>(string store, string key);
    Task<IReadOnlyList<T>> GetAllAsync<T>(string store);
    Task PutAsync<T>(string store, string key, T value);
    Task DeleteAsync(string store, string key);
    Task ClearAsync(string store);
}

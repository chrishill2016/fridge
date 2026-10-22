namespace FridgeChef.Services.Browser;

public interface IConnectivityService
{
    bool IsOnline { get; }

    /// <summary>Raised with the new state when the browser goes online or offline.</summary>
    event Action<bool>? Changed;

    Task InitializeAsync();
}

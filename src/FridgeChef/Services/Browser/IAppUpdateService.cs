namespace FridgeChef.Services.Browser;

public interface IAppUpdateService
{
    bool UpdateAvailable { get; }

    event Action? UpdateAvailableChanged;

    /// <summary>Starts watching the service worker for a new version.</summary>
    Task InitializeAsync();

    /// <summary>Activates the waiting service worker and reloads the page.</summary>
    Task ApplyUpdateAsync();
}

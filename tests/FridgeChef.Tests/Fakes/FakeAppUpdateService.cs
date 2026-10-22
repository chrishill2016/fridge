using FridgeChef.Services.Browser;

namespace FridgeChef.Tests.Fakes;

public sealed class FakeAppUpdateService : IAppUpdateService
{
    public bool UpdateAvailable { get; private set; }
    public int ApplyCount { get; private set; }

    public event Action? UpdateAvailableChanged;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task ApplyUpdateAsync()
    {
        ApplyCount++;
        return Task.CompletedTask;
    }

    public void RaiseUpdateAvailable()
    {
        UpdateAvailable = true;
        UpdateAvailableChanged?.Invoke();
    }
}

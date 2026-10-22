using FridgeChef.Services.Browser;

namespace FridgeChef.Tests.Fakes;

public sealed class FakeConnectivityService(bool isOnline = true) : IConnectivityService
{
    public bool IsOnline { get; private set; } = isOnline;

    public event Action<bool>? Changed;

    public Task InitializeAsync() => Task.CompletedTask;

    public void Set(bool isOnline)
    {
        IsOnline = isOnline;
        Changed?.Invoke(isOnline);
    }
}

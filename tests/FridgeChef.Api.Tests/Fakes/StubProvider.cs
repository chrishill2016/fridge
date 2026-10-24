using FridgeChef.Api.Foundry;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Tests.Fakes;

public sealed class StubProvider(IChatClient client) : IChatClientProvider
{
    public List<string> RequestedModels { get; } = [];

    public IChatClient GetChatClient(string model)
    {
        RequestedModels.Add(model);
        return client;
    }
}

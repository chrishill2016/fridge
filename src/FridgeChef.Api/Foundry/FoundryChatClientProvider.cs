using System.Collections.Concurrent;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Foundry;

public interface IChatClientProvider
{
    IChatClient GetChatClient(string model);
}

/// <summary>
/// One IChatClient per model deployment, talking to the Foundry project's Responses API.
/// Stored output is disabled so photos and prompts aren't kept server-side.
/// </summary>
public sealed class FoundryChatClientProvider(AIProjectClient project) : IChatClientProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, IChatClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public IChatClient GetChatClient(string model) =>
        _clients.GetOrAdd(model, Create);

    // The Responses API is still flagged as evaluation-only across the OpenAI/Foundry .NET SDKs,
    // and it's the only way a Foundry project exposes model calls. Re-check on SDK upgrades.
#pragma warning disable MAAI001
    private IChatClient Create(string model) =>
        project.ProjectOpenAIClient
            .GetProjectResponsesClient()
            .AsIChatClientWithStoredOutputDisabled(model, includeReasoningEncryptedContent: false);
#pragma warning restore MAAI001

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
    }
}

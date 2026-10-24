using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Tests.Fakes;

/// <summary>Returns canned text and records what it was asked.</summary>
public sealed class FakeChatClient(string responseText) : IChatClient
{
    public List<ChatMessage> LastMessages { get; } = [];
    public ChatOptions? LastOptions { get; private set; }
    public UsageDetails Usage { get; set; } = new() { InputTokenCount = 1200, OutputTokenCount = 80 };

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages.Clear();
        LastMessages.AddRange(messages);
        LastOptions = options;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)) { Usage = Usage });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

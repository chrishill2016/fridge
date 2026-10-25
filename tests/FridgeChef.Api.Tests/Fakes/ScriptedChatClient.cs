using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Tests.Fakes;

/// <summary>Replies with each scripted response in turn and records every call.</summary>
public sealed class ScriptedChatClient(params ChatResponse[] script) : IChatClient
{
    private int _next;

    public List<(List<ChatMessage> Messages, ChatOptions? Options)> Calls { get; } = [];

    public static ChatResponse Text(string text, long input = 100, long output = 20) =>
        new(new ChatMessage(ChatRole.Assistant, text)) { Usage = new() { InputTokenCount = input, OutputTokenCount = output } };

    public static ChatResponse ToolCall(string callId, string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]))
        {
            Usage = new() { InputTokenCount = 50, OutputTokenCount = 10 },
        };

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(([.. messages], options));
        if (_next >= script.Length)
        {
            throw new InvalidOperationException("The script ran out of responses.");
        }

        return Task.FromResult(script[_next++]);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

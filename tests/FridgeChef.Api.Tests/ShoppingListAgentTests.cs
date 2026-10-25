using FridgeChef.Api.Features.ShoppingList;
using FridgeChef.Api.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Tests;

public class ShoppingListAgentTests
{
    [Fact]
    public async Task Agent_runs_the_tool_for_each_call_and_returns_collected_items()
    {
        var chat = new ScriptedChatClient(
            ScriptedChatClient.ToolCall("c1", "AddToShoppingList", new() { ["item"] = "double cream", ["quantity"] = "300ml" }),
            ScriptedChatClient.ToolCall("c2", "AddToShoppingList", new() { ["item"] = "spinach" }),
            ScriptedChatClient.Text("Added double cream and spinach."));
        var sut = new ShoppingListAgent(new StubProvider(chat));

        var result = await sut.PlanAsync("Creamy pasta", ["cream", "spinach"], ["milk"], "gpt-4.1-mini", TestContext.Current.CancellationToken);

        Assert.Equal(
            [new("double cream", "300ml"), new("spinach", null)],
            result.Items);
        Assert.Equal(3, chat.Calls.Count);

        // First call: the tool is offered, with instructions and the prompt.
        var (firstMessages, firstOptions) = chat.Calls[0];
        Assert.Contains(firstOptions!.Tools!, t => t.Name == "AddToShoppingList");
        Assert.Contains("Call AddToShoppingList", firstOptions.Instructions);
        Assert.Contains("Already on my shopping list: milk", string.Join("\n", firstMessages.Select(m => m.Text)));

        // Later calls carry the tool's result back to the model.
        var toolResult = chat.Calls[1].Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Single();
        Assert.Equal("c1", toolResult.CallId);
        Assert.Contains("Added", toolResult.Result?.ToString());

        Assert.Equal("shopping-v1", result.Run.PromptVersion);
        Assert.Equal(50 + 50 + 100, result.Run.InputTokens);
    }

    [Fact]
    public void Tool_trims_dedupes_and_caps()
    {
        var tools = new ShoppingListTools();

        Assert.StartsWith("Added", tools.AddToShoppingList(" Onions ", " 3 "));
        Assert.Contains("already", tools.AddToShoppingList("onions"));
        Assert.Contains("empty", tools.AddToShoppingList("  "));
        for (var i = 0; i < 40; i++)
        {
            tools.AddToShoppingList($"item {i}");
        }

        Assert.Equal(new("Onions", "3"), tools.Items[0]);
        Assert.Equal(FridgeChef.Shared.ApiLimits.MaxListItems, tools.Items.Count);
    }
}

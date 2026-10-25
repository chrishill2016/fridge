using System.Text;
using FridgeChef.Api.Foundry;
using FridgeChef.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FridgeChef.Api.Features.ShoppingList;

public interface IShoppingListAgent
{
    Task<ShoppingListResponse> PlanAsync(string recipeTitle, IReadOnlyList<string> missing, IReadOnlyList<string> alreadyOnList, string model, CancellationToken cancellationToken);
}

/// <summary>
/// An Agent Framework agent with one function tool. The framework runs the tool-call loop:
/// model asks for AddToShoppingList → our C# method runs → result goes back → repeat until done.
/// </summary>
public sealed class ShoppingListAgent(IChatClientProvider chatClients) : IShoppingListAgent
{
    internal const string PromptVersion = "shopping-v1";

    internal const string Instructions = """
        You turn a recipe's missing ingredients into a tidy shopping list.
        - Call AddToShoppingList once for each thing the user needs to buy.
        - Use the name you'd find in a UK supermarket and a sensible pack size as the quantity (e.g. "double cream", "300ml").
        - Skip anything already on their shopping list, and anything that isn't bought (e.g. water).
        - Combine duplicates (e.g. "1 onion" and "2 onions" → one "onions").
        - When you've added everything, reply with a one-line summary.
        """;

    public async Task<ShoppingListResponse> PlanAsync(string recipeTitle, IReadOnlyList<string> missing, IReadOnlyList<string> alreadyOnList, string model, CancellationToken cancellationToken)
    {
        var tools = new ShoppingListTools();
        var agent = new ChatClientAgent(chatClients.GetChatClient(model), new ChatClientAgentOptions
        {
            Name = "shopping-list-helper",
            Description = "Adds a recipe's missing ingredients to the shopping list.",
            ChatOptions = new ChatOptions
            {
                ModelId = model,
                Instructions = Instructions,
                Tools = [AIFunctionFactory.Create(tools.AddToShoppingList)],
            },
        });

        var response = await agent.RunAsync(BuildPrompt(recipeTitle, missing, alreadyOnList), cancellationToken: cancellationToken);

        return new ShoppingListResponse(
            tools.Items,
            new AiRunInfo(model, PromptVersion, response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount));
    }

    internal static string BuildPrompt(string recipeTitle, IReadOnlyList<string> missing, IReadOnlyList<string> alreadyOnList)
    {
        var prompt = new StringBuilder()
            .AppendLine($"Recipe: {recipeTitle}")
            .AppendLine("Missing ingredients:")
            .AppendJoin('\n', missing.Select(m => "- " + m)).AppendLine();
        prompt.AppendLine(alreadyOnList.Count == 0
            ? "My shopping list is empty."
            : "Already on my shopping list: " + string.Join(", ", alreadyOnList));
        return prompt.ToString();
    }
}

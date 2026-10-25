using FridgeChef.Services.Api;
using FridgeChef.Services.Browser;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

public sealed record AddMissingResult(int Added, bool UsedAi, string? Warning = null);

/// <summary>
/// Adds a recipe's missing ingredients to the shopping list. Online, the shopping-list agent tidies
/// them first (shop names, pack sizes, skips what's already listed); offline or on failure, they're
/// added as they are, so the button always does something useful.
/// </summary>
public sealed class ShoppingListAssistant(
    IShoppingListService shoppingList,
    IFridgeChefApi api,
    IConnectivityService connectivity,
    ISettingsService settings)
{
    public async Task<AddMissingResult> AddMissingAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        if (recipe.Missing.Count == 0)
        {
            return new AddMissingResult(0, false);
        }

        var before = (await shoppingList.GetItemsAsync()).Count;
        string? warning = null;
        var usedAi = false;

        if (connectivity.IsOnline)
        {
            try
            {
                var onList = (await shoppingList.GetItemsAsync()).Where(i => !i.Bought).Select(i => i.Name).ToList();
                var model = (await settings.GetAppSettingsAsync()).Model;
                var plan = await api.PlanShoppingAsync(new ShoppingListRequest(recipe.Title, recipe.Missing, onList, model), cancellationToken);
                foreach (var item in plan.Items)
                {
                    await shoppingList.AddAsync(item.Name, recipe.Title, item.Quantity);
                }

                usedAi = true;
            }
            catch (ApiException ex)
            {
                warning = ex.Message;
            }
        }

        if (!usedAi)
        {
            await shoppingList.AddRangeAsync(recipe.Missing, recipe.Title);
        }

        return new AddMissingResult((await shoppingList.GetItemsAsync()).Count - before, usedAi, warning);
    }
}

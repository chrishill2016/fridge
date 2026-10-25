using System.ComponentModel;
using FridgeChef.Shared;
using FridgeChef.Shared.Models;

namespace FridgeChef.Api.Features.ShoppingList;

/// <summary>
/// The tool the shopping-list agent calls. The shopping list lives on the device, so calls are
/// collected for this request and returned to the app, which saves them.
/// </summary>
public sealed class ShoppingListTools
{
    private readonly List<ShoppingSuggestion> _items = [];

    public IReadOnlyList<ShoppingSuggestion> Items => _items;

    [Description("Add one item to the user's shopping list.")]
    public string AddToShoppingList(
        [Description("What to buy, as a short shopping name, e.g. \"chopped tomatoes\"")] string item,
        [Description("How much to buy, e.g. \"1 tin\" or \"200g\"; omit if unclear")] string? quantity = null)
    {
        var name = item.Trim();
        if (name.Length == 0)
        {
            return "Not added: the item name was empty.";
        }

        if (_items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"\"{name}\" is already on the list.";
        }

        if (_items.Count >= ApiLimits.MaxListItems)
        {
            return "Not added: the list is full for this request.";
        }

        _items.Add(new ShoppingSuggestion(
            name.Length > ApiLimits.MaxTextLength ? name[..ApiLimits.MaxTextLength] : name,
            string.IsNullOrWhiteSpace(quantity) ? null : quantity.Trim()));
        return $"Added \"{name}\".";
    }
}

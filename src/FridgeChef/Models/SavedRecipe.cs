using FridgeChef.Shared.Models;

namespace FridgeChef.Models;

public sealed record SavedRecipe(string Id, Recipe Recipe, DateTimeOffset SavedUtc);

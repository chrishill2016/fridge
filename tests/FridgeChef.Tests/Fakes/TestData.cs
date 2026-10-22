using FridgeChef.Shared.Models;

namespace FridgeChef.Tests.Fakes;

public static class TestData
{
    public static Recipe Recipe(string title = "Cheese omelette", params string[] missing) =>
        new(title, 15, ["3 eggs", "50g cheddar"], missing, ["Whisk the eggs.", "Cook and fold in the cheese."]);
}

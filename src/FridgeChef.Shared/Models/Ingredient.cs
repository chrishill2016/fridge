namespace FridgeChef.Shared.Models;

/// <summary>An ingredient spotted in a photo. Confidence is 0–1 from the vision model.</summary>
public sealed record Ingredient(string Name, string? Quantity = null, double Confidence = 1.0);

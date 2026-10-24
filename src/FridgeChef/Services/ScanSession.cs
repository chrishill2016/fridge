using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services;

/// <summary>
/// The current photo and the user's edited ingredient list. Held in memory so it survives moving
/// between pages; the recipes step (phase 4) reads the ingredients from here.
/// </summary>
public sealed class ScanSession
{
    private readonly List<Ingredient> _ingredients = [];

    public event Action? Changed;

    public PreparedPhoto? Photo { get; private set; }
    public AiRunInfo? Run { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients => _ingredients;
    public bool HasResult => Run is not null;

    public void SetPhoto(PreparedPhoto photo)
    {
        Photo = photo;
        Run = null;
        _ingredients.Clear();
        Changed?.Invoke();
    }

    public void SetResult(IngredientsResponse result)
    {
        Run = result.Run;
        _ingredients.Clear();
        _ingredients.AddRange(result.Ingredients);
        Changed?.Invoke();
    }

    /// <summary>Adds an ingredient the model missed. Returns false for blanks and duplicates.</summary>
    public bool Add(string name)
    {
        var trimmed = name.Trim().ToLowerInvariant();
        if (trimmed.Length == 0 || _ingredients.Any(i => i.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _ingredients.Add(new Ingredient(trimmed));
        Changed?.Invoke();
        return true;
    }

    public void Remove(string name)
    {
        if (_ingredients.RemoveAll(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        Photo = null;
        Run = null;
        _ingredients.Clear();
        Changed?.Invoke();
    }
}

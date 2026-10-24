using Microsoft.Extensions.Options;

namespace FridgeChef.Api.Foundry;

public enum AiTask
{
    Ingredients,
    Recipes,
}

public interface IModelSelector
{
    IReadOnlyCollection<string> AllowedModels { get; }

    /// <summary>The requested deployment if allowed, the task's default if none was requested.</summary>
    /// <exception cref="ModelNotAllowedException">The requested deployment isn't on the allow-list.</exception>
    string Resolve(AiTask task, string? requested);
}

public sealed class ModelNotAllowedException(string model)
    : Exception($"Model '{model}' is not allowed.")
{
    public string Model { get; } = model;
}

public sealed class ModelSelector(IOptions<FoundryOptions> options) : IModelSelector
{
    private readonly FoundryOptions _options = options.Value;

    public IReadOnlyCollection<string> AllowedModels { get; } =
        new[] { options.Value.IngredientsModel, options.Value.RecipesModel }
            .Concat(options.Value.AllowedModels)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public string Resolve(AiTask task, string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return task == AiTask.Ingredients ? _options.IngredientsModel : _options.RecipesModel;
        }

        // Return the configured spelling, not the caller's.
        return AllowedModels.FirstOrDefault(m => string.Equals(m, requested.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ModelNotAllowedException(requested);
    }
}

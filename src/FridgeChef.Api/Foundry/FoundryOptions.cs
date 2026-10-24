using System.ComponentModel.DataAnnotations;

namespace FridgeChef.Api.Foundry;

/// <summary>Bound from the "Foundry" config section (app settings use Foundry__ProjectEndpoint etc.).</summary>
public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    /// <summary>e.g. https://&lt;resource&gt;.services.ai.azure.com/api/projects/&lt;project&gt;</summary>
    [Required]
    public Uri ProjectEndpoint { get; set; } = default!;

    /// <summary>Deployment used for photo analysis when the request doesn't pick one.</summary>
    [Required]
    public string IngredientsModel { get; set; } = "";

    /// <summary>Deployment used for recipes when the request doesn't pick one.</summary>
    [Required]
    public string RecipesModel { get; set; } = "";

    /// <summary>Extra deployments the app may pick. The two defaults are always allowed.</summary>
    public string[] AllowedModels { get; set; } = [];
}

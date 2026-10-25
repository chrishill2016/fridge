using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

/// <summary>Calls to the Fridge Chef Function. Every method throws <see cref="ApiException"/> with a message safe to show.</summary>
public interface IFridgeChefApi
{
    /// <summary>True when no API is configured and results are canned sample data.</summary>
    bool IsDemo { get; }

    Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default);

    Task<RecipesResponse> SuggestRecipesAsync(RecipesRequest request, CancellationToken cancellationToken = default);

    Task<ShoppingListResponse> PlanShoppingAsync(ShoppingListRequest request, CancellationToken cancellationToken = default);

    Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);
}

public sealed record PreparedPhoto(byte[] Bytes, string MediaType);

public sealed class ApiException(string message, Exception? inner = null) : Exception(message, inner);

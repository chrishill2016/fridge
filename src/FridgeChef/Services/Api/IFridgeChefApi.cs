using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

public interface IFridgeChefApi
{
    /// <summary>True when no API is configured and results are canned sample data.</summary>
    bool IsDemo { get; }

    /// <exception cref="ApiException">The call failed; the message is safe to show the user.</exception>
    Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default);
}

public sealed record PreparedPhoto(byte[] Bytes, string MediaType);

public sealed class ApiException(string message, Exception? inner = null) : Exception(message, inner);

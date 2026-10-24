using System.Net.Http.Headers;
using System.Net.Http.Json;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

public sealed class HttpFridgeChefApi(HttpClient http) : IFridgeChefApi
{
    public bool IsDemo => false;

    public async Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(photo.Bytes);
        image.Headers.ContentType = new MediaTypeHeaderValue(photo.MediaType);
        content.Add(image, "image", "photo" + Extension(photo.MediaType));
        if (!string.IsNullOrWhiteSpace(model))
        {
            content.Add(new StringContent(model), "model");
        }

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync("ingredients", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Couldn't reach Fridge Chef's server. Check your connection and try again.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(await ReadProblemAsync(response, cancellationToken));
            }

            return await response.Content.ReadFromJsonAsync<IngredientsResponse>(cancellationToken)
                ?? throw new ApiException("The server sent an empty response.");
        }
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Not a problem+json body; fall through to a generic message.
        }

        return $"Something went wrong ({(int)response.StatusCode}). Try again.";
    }

    private static string Extension(string mediaType) => mediaType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg",
    };

    // The parts of an RFC 7807 problem+json body the API returns.
    private sealed record Problem(string? Detail);
}

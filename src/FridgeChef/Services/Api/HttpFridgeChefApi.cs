using System.Net.Http.Headers;
using System.Net.Http.Json;
using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

public sealed class HttpFridgeChefApi(HttpClient http) : IFridgeChefApi
{
    public bool IsDemo => false;

    public Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(photo.Bytes);
        image.Headers.ContentType = new MediaTypeHeaderValue(photo.MediaType);
        content.Add(image, "image", "photo" + Extension(photo.MediaType));
        if (!string.IsNullOrWhiteSpace(model))
        {
            content.Add(new StringContent(model), "model");
        }

        return SendAsync<IngredientsResponse>(new HttpRequestMessage(HttpMethod.Post, "ingredients") { Content = content }, cancellationToken);
    }

    public Task<RecipesResponse> SuggestRecipesAsync(RecipesRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<RecipesResponse>(new HttpRequestMessage(HttpMethod.Post, "recipes") { Content = JsonContent.Create(request) }, cancellationToken);

    public Task<ShoppingListResponse> PlanShoppingAsync(ShoppingListRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<ShoppingListResponse>(new HttpRequestMessage(HttpMethod.Post, "shopping-list") { Content = JsonContent.Create(request) }, cancellationToken);

    public Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<ModelsResponse>(new HttpRequestMessage(HttpMethod.Get, "models"), cancellationToken);

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, cancellationToken);
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

                return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                    ?? throw new ApiException("The server sent an empty response.");
            }
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

using System.Text.Json;
using FridgeChef.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FridgeChef.Api.Http;

public static class JsonBody
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Reads a small JSON body. Returns a problem result instead of throwing for bad input.</summary>
    public static async Task<(T? Value, IActionResult? Problem)> ReadAsync<T>(HttpRequest request, CancellationToken cancellationToken)
        where T : class
    {
        if (request.ContentLength > ApiLimits.MaxJsonBytes)
        {
            return (null, Problems.Create(StatusCodes.Status413PayloadTooLarge, "The request is too large."));
        }

        if (request.HasJsonContentType() is false)
        {
            return (null, Problems.Create(StatusCodes.Status400BadRequest, "Send the request as application/json."));
        }

        try
        {
            // Bounded read, in case Content-Length was missing.
            using var limited = new MemoryStream();
            await request.Body.CopyToAsync(limited, cancellationToken);
            if (limited.Length > ApiLimits.MaxJsonBytes)
            {
                return (null, Problems.Create(StatusCodes.Status413PayloadTooLarge, "The request is too large."));
            }

            limited.Position = 0;
            var value = await JsonSerializer.DeserializeAsync<T>(limited, Options, cancellationToken);
            return value is null
                ? (null, Problems.Create(StatusCodes.Status400BadRequest, "The request body is empty."))
                : (value, null);
        }
        catch (JsonException)
        {
            return (null, Problems.Create(StatusCodes.Status400BadRequest, "The request body isn't valid JSON."));
        }
    }
}

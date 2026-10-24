using FridgeChef.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace FridgeChef.Services.Api;

public interface IPhotoPreparer
{
    /// <exception cref="PhotoException">The file isn't a usable image; the message is safe to show.</exception>
    Task<PreparedPhoto> PrepareAsync(IBrowserFile file, CancellationToken cancellationToken = default);
}

public sealed class PhotoException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Shrinks photos in the browser (canvas, via Blazor's RequestImageFileAsync) before upload: phone
/// photos are often 5–12 MB, the API takes 4 MB, and the model doesn't need more than ~1600px.
/// </summary>
public sealed class BrowserPhotoPreparer : IPhotoPreparer
{
    private static readonly int[] MaxSides = [1600, 1024];

    public async Task<PreparedPhoto> PrepareAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new PhotoException("That file isn't a photo.");
        }

        foreach (var side in MaxSides)
        {
            IBrowserFile resized;
            try
            {
                resized = await file.RequestImageFileAsync("image/jpeg", side, side);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new PhotoException("Couldn't read that photo. Try a JPEG or PNG.", ex);
            }

            if (resized.Size <= ApiLimits.MaxImageBytes)
            {
                using var buffer = new MemoryStream((int)resized.Size);
                await using var stream = resized.OpenReadStream(ApiLimits.MaxImageBytes, cancellationToken);
                await stream.CopyToAsync(buffer, cancellationToken);
                return new PreparedPhoto(buffer.ToArray(), "image/jpeg");
            }
        }

        throw new PhotoException("That photo is too large even after shrinking it.");
    }
}

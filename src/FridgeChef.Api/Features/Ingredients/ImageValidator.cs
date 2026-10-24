using FridgeChef.Shared;
using Microsoft.AspNetCore.Http;

namespace FridgeChef.Api.Features.Ingredients;

public sealed record ValidatedImage(byte[] Bytes, string MediaType);

/// <summary>
/// Checks an uploaded photo. The type comes from the file's leading bytes, not the client's
/// Content-Type, so a renamed file can't slip through.
/// </summary>
public static class ImageValidator
{
    public static async Task<(ValidatedImage? Image, string? Error)> ValidateAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "Attach a photo in the 'image' form field.");
        }

        if (file.Length > ApiLimits.MaxImageBytes)
        {
            return (null, $"The photo is too large. The limit is {ApiLimits.MaxImageBytes / (1024 * 1024)} MB.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var mediaType = Sniff(bytes);
        return mediaType is null
            ? (null, "The photo must be a JPEG, PNG or WebP image.")
            : (new ValidatedImage(bytes, mediaType), null);
    }

    internal static string? Sniff(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return "image/jpeg";
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return "image/png";
        }

        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }
}

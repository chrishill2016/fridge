using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Tests.Fakes;
using FridgeChef.Shared;
using Microsoft.AspNetCore.Http;

namespace FridgeChef.Api.Tests;

public class ImageValidatorTests
{
    public static TheoryData<byte[], string> ValidImages => new()
    {
        { TestImages.Jpeg(), "image/jpeg" },
        { TestImages.Png(), "image/png" },
        { TestImages.Webp(), "image/webp" },
    };

    [Theory]
    [MemberData(nameof(ValidImages))]
    public async Task Accepts_supported_types_by_content(byte[] bytes, string expected)
    {
        // Declared type is deliberately wrong: only the bytes count.
        var (image, error) = await ImageValidator.ValidateAsync(File(bytes, "application/octet-stream"), TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.Equal(expected, image!.MediaType);
        Assert.Equal(bytes, image.Bytes);
    }

    [Fact]
    public async Task Rejects_unsupported_type_even_if_declared_as_jpeg()
    {
        var (image, error) = await ImageValidator.ValidateAsync(File(TestImages.Gif(), "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Null(image);
        Assert.Contains("JPEG, PNG or WebP", error);
    }

    [Fact]
    public async Task Rejects_missing_or_empty_file()
    {
        Assert.NotNull((await ImageValidator.ValidateAsync(null, TestContext.Current.CancellationToken)).Error);
        Assert.NotNull((await ImageValidator.ValidateAsync(File([], "image/jpeg"), TestContext.Current.CancellationToken)).Error);
    }

    [Fact]
    public async Task Rejects_file_over_limit()
    {
        var (image, error) = await ImageValidator.ValidateAsync(
            File(TestImages.Jpeg((int)ApiLimits.MaxImageBytes + 1), "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Null(image);
        Assert.Contains("too large", error);
    }

    [Fact]
    public async Task Accepts_file_exactly_at_limit()
    {
        var (image, _) = await ImageValidator.ValidateAsync(
            File(TestImages.Jpeg((int)ApiLimits.MaxImageBytes), "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.NotNull(image);
    }

    [Fact]
    public void Sniff_requires_full_webp_header() =>
        Assert.Null(ImageValidator.Sniff("RIFF\0\0\0\0WAVE"u8));

    private static FormFile File(byte[] bytes, string contentType) =>
        new(new MemoryStream(bytes), 0, bytes.Length, "image", "photo")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
}

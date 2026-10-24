using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Foundry;
using FridgeChef.Api.Tests.Fakes;
using FridgeChef.Shared;
using FridgeChef.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace FridgeChef.Api.Tests;

public class IngredientsFunctionTests
{
    private readonly RecordingDetector _detector = new();
    private readonly IngredientsFunction _sut;

    public IngredientsFunctionTests() =>
        _sut = new IngredientsFunction(_detector, new ModelSelector(TestOptions.Foundry("phi-4")), NullLogger<IngredientsFunction>.Instance);

    [Fact]
    public async Task Valid_photo_returns_ingredients_using_default_model()
    {
        var result = await _sut.Run(Request(TestImages.Jpeg()), TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<IngredientsResponse>(ok.Value);
        Assert.Equal("eggs", Assert.Single(body.Ingredients).Name);
        Assert.Equal("gpt-4.1-mini", _detector.Model);
        Assert.Equal("image/jpeg", _detector.Image!.MediaType);
    }

    [Fact]
    public async Task Requested_model_is_used_when_allowed()
    {
        await _sut.Run(Request(TestImages.Png(), model: "phi-4"), TestContext.Current.CancellationToken);

        Assert.Equal("phi-4", _detector.Model);
    }

    [Fact]
    public async Task Unlisted_model_is_400_and_never_calls_foundry()
    {
        var result = await _sut.Run(Request(TestImages.Png(), model: "gpt-5-pro"), TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Null(_detector.Model);
    }

    [Fact]
    public async Task Non_image_is_400()
    {
        var result = await _sut.Run(Request(TestImages.Gif()), TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Null(_detector.Model);
    }

    [Fact]
    public async Task Missing_image_is_400()
    {
        var result = await _sut.Run(Request(null), TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Non_form_body_is_400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";

        AssertProblem(await _sut.Run(context.Request, TestContext.Current.CancellationToken), StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Oversized_request_is_413_before_reading_body()
    {
        var request = Request(TestImages.Jpeg());
        request.ContentLength = IngredientsFunction.MaxRequestBytes + 1;

        AssertProblem(await _sut.Run(request, TestContext.Current.CancellationToken), StatusCodes.Status413PayloadTooLarge);
        Assert.Null(_detector.Model);
    }

    [Fact]
    public async Task Foundry_failure_is_502_without_leaking_details()
    {
        _detector.Throw = new InvalidOperationException("secret internal detail");

        var result = await _sut.Run(Request(TestImages.Jpeg()), TestContext.Current.CancellationToken);

        var problem = AssertProblem(result, StatusCodes.Status502BadGateway);
        Assert.DoesNotContain("secret", problem.Detail);
    }

    private static HttpRequest Request(byte[]? image, string? model = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=test";

        var files = new FormFileCollection();
        if (image is not null)
        {
            files.Add(new FormFile(new MemoryStream(image), 0, image.Length, "image", "photo.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg",
            });
        }

        var fields = new Dictionary<string, StringValues>();
        if (model is not null)
        {
            fields["model"] = model;
        }

        request.Form = new FormCollection(fields, files);
        return request;
    }

    private static ProblemDetails AssertProblem(IActionResult result, int status)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
        return Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    private sealed class RecordingDetector : IIngredientDetector
    {
        public string? Model { get; private set; }
        public ValidatedImage? Image { get; private set; }
        public Exception? Throw { get; set; }

        public Task<IngredientsResponse> DetectAsync(ValidatedImage image, string model, CancellationToken cancellationToken)
        {
            Model = model;
            Image = image;
            if (Throw is not null)
            {
                throw Throw;
            }

            return Task.FromResult(new IngredientsResponse(
                [new Ingredient("eggs", "6", 0.9)],
                new AiRunInfo(model, "test", 1, 1)));
        }
    }
}

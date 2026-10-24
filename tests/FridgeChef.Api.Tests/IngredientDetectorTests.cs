using FridgeChef.Api.Features.Ingredients;
using FridgeChef.Api.Tests.Fakes;
using Microsoft.Extensions.AI;
using static FridgeChef.Api.Features.Ingredients.IngredientDetector;

namespace FridgeChef.Api.Tests;

public class IngredientDetectorTests
{
    private const string ModelJson = """
        {"ingredients":[
          {"name":" Eggs ","quantity":"6","confidence":0.95},
          {"name":"cheddar","quantity":"","confidence":0.7},
          {"name":"eggs","quantity":null,"confidence":0.5},
          {"name":"","quantity":null,"confidence":0.9},
          {"name":"milk","quantity":"half a bottle","confidence":1.4}
        ]}
        """;

    [Fact]
    public async Task Sends_instructions_photo_and_model_and_cleans_result()
    {
        var chat = new FakeChatClient(ModelJson);
        var provider = new StubProvider(chat);
        var sut = new IngredientDetector(provider);
        var image = new ValidatedImage(TestImages.Png(), "image/png");

        var result = await sut.DetectAsync(image, "gpt-4.1-mini", TestContext.Current.CancellationToken);

        Assert.Equal(["gpt-4.1-mini"], provider.RequestedModels);
        Assert.Equal("gpt-4.1-mini", chat.LastOptions?.ModelId);
        Assert.IsType<ChatResponseFormatJson>(chat.LastOptions?.ResponseFormat);
        Assert.Contains(chat.LastMessages, m => m.Role == ChatRole.System && m.Text.Contains("ingredients"));
        var photo = Assert.Single(chat.LastMessages.SelectMany(m => m.Contents).OfType<DataContent>());
        Assert.Equal("image/png", photo.MediaType);
        Assert.Equal(image.Bytes, photo.Data.ToArray());

        Assert.Equal(["milk", "eggs", "cheddar"], result.Ingredients.Select(i => i.Name));
        Assert.Equal(1.0, result.Ingredients[0].Confidence);
        Assert.Equal("6", result.Ingredients[1].Quantity);
        Assert.Null(result.Ingredients[2].Quantity);

        Assert.Equal("gpt-4.1-mini", result.Run.Model);
        Assert.Equal(PromptVersion, result.Run.PromptVersion);
        Assert.Equal(1200, result.Run.InputTokens);
        Assert.Equal(80, result.Run.OutputTokens);
    }

    [Fact]
    public void Clean_handles_null_nan_and_caps_count()
    {
        Assert.Empty(Clean(null));

        var nan = Clean([new DetectedIngredient("salt", null, double.NaN)]);
        Assert.Equal(0, Assert.Single(nan).Confidence);

        var many = Clean(Enumerable.Range(0, 100).Select(i => new DetectedIngredient($"item {i}", null, 0.5)));
        Assert.Equal(MaxIngredients, many.Count);
    }
}

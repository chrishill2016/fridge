using FridgeChef.Api.Foundry;
using FridgeChef.Api.Tests.Fakes;

namespace FridgeChef.Api.Tests;

public class ModelSelectorTests
{
    private readonly ModelSelector _sut = new(TestOptions.Foundry("mistral-medium"));

    [Theory]
    [InlineData(AiTask.Ingredients, null, "gpt-4.1-mini")]
    [InlineData(AiTask.Ingredients, "  ", "gpt-4.1-mini")]
    [InlineData(AiTask.Recipes, null, "gpt-4.1")]
    public void Uses_task_default_when_nothing_requested(AiTask task, string? requested, string expected) =>
        Assert.Equal(expected, _sut.Resolve(task, requested));

    [Theory]
    [InlineData("mistral-medium", "mistral-medium")]
    [InlineData(" MISTRAL-medium ", "mistral-medium")]
    [InlineData("gpt-4.1", "gpt-4.1")]
    public void Allows_listed_models_and_returns_configured_spelling(string requested, string expected) =>
        Assert.Equal(expected, _sut.Resolve(AiTask.Ingredients, requested));

    [Fact]
    public void Rejects_unlisted_model()
    {
        var ex = Assert.Throws<ModelNotAllowedException>(() => _sut.Resolve(AiTask.Ingredients, "gpt-5-pro"));
        Assert.Equal("gpt-5-pro", ex.Model);
    }

    [Fact]
    public void Allowed_models_include_defaults_without_duplicates()
    {
        var sut = new ModelSelector(TestOptions.Foundry("gpt-4.1-mini", "", "phi-4"));

        Assert.Equal(["gpt-4.1-mini", "gpt-4.1", "phi-4"], sut.AllowedModels);
    }
}

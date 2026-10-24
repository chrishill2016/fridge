using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;

namespace FridgeChef.Tests.Fakes;

public sealed class FakeApi : IFridgeChefApi
{
    public bool IsDemo { get; set; }
    public Exception? Throw { get; set; }
    public PreparedPhoto? LastPhoto { get; private set; }

    public IngredientsResponse Response { get; set; } = new(
        [new Ingredient("eggs", "6", 0.95), new Ingredient("yoghurt", null, 0.4)],
        new AiRunInfo("gpt-4.1-mini", "ingredients-v1", 1000, 50));

    public Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        LastPhoto = photo;
        return Throw is null ? Task.FromResult(Response) : Task.FromException<IngredientsResponse>(Throw);
    }
}

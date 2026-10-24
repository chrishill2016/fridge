using FridgeChef.Shared.Models;

namespace FridgeChef.Services.Api;

/// <summary>Stands in for the API until one is deployed, so the app can be tried end to end.</summary>
public sealed class DemoFridgeChefApi(TimeProvider time) : IFridgeChefApi
{
    internal static readonly TimeSpan Delay = TimeSpan.FromSeconds(1.5);

    public bool IsDemo => true;

    public async Task<IngredientsResponse> AnalysePhotoAsync(PreparedPhoto photo, string? model = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, time, cancellationToken);
        return new IngredientsResponse(
            [
                new("eggs", "6", 0.97),
                new("cheddar", "half a block", 0.92),
                new("milk", "1 bottle", 0.9),
                new("spring onions", null, 0.81),
                new("cherry tomatoes", "1 punnet", 0.78),
                new("butter", null, 0.74),
                new("greek yoghurt", null, 0.55),
            ],
            new AiRunInfo("demo", "demo", null, null));
    }
}

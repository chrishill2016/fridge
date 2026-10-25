using FridgeChef.Shared;

namespace FridgeChef.Api.Http;

/// <summary>Normalises user-supplied lists before they go into a prompt.</summary>
public static class TextList
{
    public static IReadOnlyList<string> Clean(IEnumerable<string?>? items, int max) =>
        [.. (items ?? [])
            .Select(i => i?.Trim() ?? "")
            .Where(i => i.Length > 0)
            .Select(i => i.Length > ApiLimits.MaxTextLength ? i[..ApiLimits.MaxTextLength] : i)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)];
}

using FridgeChef.Services;
using FridgeChef.Services.Api;
using FridgeChef.Shared.Models;

namespace FridgeChef.Tests.Services;

public class ScanSessionTests
{
    private readonly ScanSession _sut = new();
    private int _changes;

    public ScanSessionTests() => _sut.Changed += () => _changes++;

    [Fact]
    public void New_photo_clears_previous_result()
    {
        _sut.SetPhoto(new PreparedPhoto([1], "image/jpeg"));
        _sut.SetResult(Result("eggs"));

        _sut.SetPhoto(new PreparedPhoto([2], "image/jpeg"));

        Assert.False(_sut.HasResult);
        Assert.Empty(_sut.Ingredients);
        Assert.Equal(3, _changes);
    }

    [Fact]
    public void Add_normalises_and_rejects_blanks_and_duplicates()
    {
        _sut.SetResult(Result("eggs"));

        Assert.True(_sut.Add("  Bacon "));
        Assert.False(_sut.Add("EGGS"));
        Assert.False(_sut.Add(" "));

        Assert.Equal(["eggs", "bacon"], _sut.Ingredients.Select(i => i.Name));
        Assert.Equal(1.0, _sut.Ingredients[1].Confidence);
    }

    [Fact]
    public void Remove_ignores_case_and_only_raises_when_something_changed()
    {
        _sut.SetResult(Result("eggs", "milk"));
        _changes = 0;

        _sut.Remove("MILK");
        _sut.Remove("bread");

        Assert.Equal(["eggs"], _sut.Ingredients.Select(i => i.Name));
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void Clear_resets_everything()
    {
        _sut.SetPhoto(new PreparedPhoto([1], "image/jpeg"));
        _sut.SetResult(Result("eggs"));

        _sut.Clear();

        Assert.Null(_sut.Photo);
        Assert.False(_sut.HasResult);
        Assert.Empty(_sut.Ingredients);
    }

    private static IngredientsResponse Result(params string[] names) =>
        new([.. names.Select(n => new Ingredient(n, null, 0.9))], new AiRunInfo("m", "v", 1, 1));
}

using FridgeChef.Models;
using FridgeChef.Services;
using FridgeChef.Shared.Models;
using FridgeChef.Tests.Fakes;

namespace FridgeChef.Tests.Services;

public class SettingsServiceTests
{
    private readonly SettingsService _sut = new(new InMemoryLocalStore());

    [Fact]
    public async Task Defaults_when_nothing_stored()
    {
        var app = await _sut.GetAppSettingsAsync();
        var preferences = await _sut.GetPreferencesAsync();

        Assert.True(app.DarkMode);
        Assert.Empty(preferences.Dislikes);
        Assert.False(preferences.QuickWeeknight);
        Assert.Equal(RecipePreferences.DefaultServings, preferences.Servings);
    }

    [Fact]
    public async Task SaveAppSettings_persists_and_raises_event()
    {
        AppSettings? raised = null;
        _sut.AppSettingsChanged += s => raised = s;

        await _sut.SaveAppSettingsAsync(new AppSettings(DarkMode: false));

        Assert.False((await _sut.GetAppSettingsAsync()).DarkMode);
        Assert.False(raised?.DarkMode);
    }

    [Fact]
    public async Task SavePreferences_normalises_and_persists()
    {
        var saved = await _sut.SavePreferencesAsync(new RecipePreferences([" mushrooms ", "Mushrooms", "", "olives"], true, 4));
        var loaded = await _sut.GetPreferencesAsync();

        Assert.Equal(["mushrooms", "olives"], saved.Dislikes);
        Assert.Equal(saved.Dislikes, loaded.Dislikes);
        Assert.True(loaded.QuickWeeknight);
        Assert.Equal(4, loaded.Servings);
    }

    [Theory]
    [InlineData(0, RecipePreferences.MinServings)]
    [InlineData(-3, RecipePreferences.MinServings)]
    [InlineData(99, RecipePreferences.MaxServings)]
    [InlineData(6, 6)]
    public void Normalise_clamps_servings(int requested, int expected) =>
        Assert.Equal(expected, SettingsService.Normalise(new RecipePreferences([], Servings: requested)).Servings);
}

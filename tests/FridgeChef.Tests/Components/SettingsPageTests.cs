using FridgeChef.Features.Settings;
using FridgeChef.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Tests.Components;

public class SettingsPageTests : AppTestContext
{
    private ISettingsService Settings => Services.GetRequiredService<ISettingsService>();

    [Fact]
    public async Task Saving_preferences_persists_dislikes()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForElement("input[data-testid=new-dislike]").Input("Mushrooms");
        cut.Find("[data-testid=add-dislike]").Click();
        cut.Find("input[data-testid=new-dislike]").Input("mushrooms");
        cut.Find("[data-testid=add-dislike]").Click();
        cut.Find("[data-testid=save-preferences]").Click();

        Assert.Equal(["Mushrooms"], (await Settings.GetPreferencesAsync()).Dislikes);
    }

    [Fact]
    public async Task Dark_mode_defaults_on_and_toggle_persists()
    {
        var cut = Render<SettingsPage>();
        var toggle = cut.WaitForElement("input[data-testid=dark-mode]");
        Assert.True(toggle.HasAttribute("checked") || toggle.GetAttribute("aria-checked") == "true");

        toggle.Change(false);

        Assert.False((await Settings.GetAppSettingsAsync()).DarkMode);
    }
}

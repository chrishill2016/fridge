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

    [Fact]
    public async Task Model_picker_lists_models_and_saves_choice()
    {
        var cut = Render<SettingsPage>();
        cut.WaitForElement("[data-testid=model-select]");

        await cut.InvokeAsync(() => cut.FindComponent<MudBlazor.MudSelect<string>>().Instance.ValueChanged.InvokeAsync("phi-4"));

        Assert.Equal("phi-4", (await Settings.GetAppSettingsAsync()).Model);

        await cut.InvokeAsync(() => cut.FindComponent<MudBlazor.MudSelect<string>>().Instance.ValueChanged.InvokeAsync(""));
        Assert.Null((await Settings.GetAppSettingsAsync()).Model);
    }

    [Fact]
    public async Task Saved_model_no_longer_offered_falls_back_to_default()
    {
        await Settings.SaveAppSettingsAsync(new FridgeChef.Models.AppSettings(Model: "retired-model"));

        var cut = Render<SettingsPage>();

        cut.WaitForElement("[data-testid=model-select]");
        Assert.Null((await Settings.GetAppSettingsAsync()).Model);
    }

    [Fact]
    public void Offline_explains_why_models_are_missing()
    {
        Connectivity.Set(false);

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Connect to the internet", cut.Find("[data-testid=models-unavailable]").TextContent));
    }

    [Fact]
    public void Demo_mode_explains_instead_of_offering_models()
    {
        Api.IsDemo = true;

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Demo mode", cut.Find("[data-testid=models-unavailable]").TextContent));
        Assert.Empty(cut.FindAll("[data-testid=model-select]"));
    }
}

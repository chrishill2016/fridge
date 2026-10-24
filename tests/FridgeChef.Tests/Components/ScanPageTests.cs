using FridgeChef.Features.Scan;
using FridgeChef.Services.Api;
using Microsoft.AspNetCore.Components.Forms;

namespace FridgeChef.Tests.Components;

public class ScanPageTests : AppTestContext
{
    private static readonly byte[] Photo = [0xFF, 0xD8, 0xFF, 1, 2, 3];

    [Fact]
    public void Analyse_is_disabled_until_a_photo_is_chosen()
    {
        var cut = Render<ScanPage>();

        Assert.True(cut.Find("[data-testid=analyse-button]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-testid=preview]"));
    }

    [Fact]
    public void Choosing_a_photo_shows_preview_and_enables_analyse()
    {
        var cut = Render<ScanPage>();

        Upload(cut);

        cut.WaitForElement("[data-testid=preview]");
        Assert.StartsWith("data:image/jpeg;base64,", cut.Find("[data-testid=preview]").GetAttribute("src"));
        Assert.False(cut.Find("[data-testid=analyse-button]").HasAttribute("disabled"));
    }

    [Fact]
    public void Analysing_lists_ingredients_marks_unsure_ones_and_shows_run_info()
    {
        var cut = Render<ScanPage>();
        Upload(cut);

        cut.WaitForElement("[data-testid=analyse-button]:not([disabled])").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=ingredient]").Count));
        Assert.Equal(Photo, Api.LastPhoto!.Bytes);
        Assert.Contains("eggs (6)", cut.Find("[data-testid=results]").TextContent);
        Assert.Contains("dashed", cut.FindAll("[data-testid=ingredient]")[1].GetAttribute("style"));
        Assert.Contains("gpt-4.1-mini · ingredients-v1 · 1,050 tokens", cut.Find("[data-testid=run-info]").TextContent);
    }

    [Fact]
    public void Ingredients_can_be_removed_and_added()
    {
        var cut = Render<ScanPage>();
        Upload(cut);
        cut.WaitForElement("[data-testid=analyse-button]:not([disabled])").Click();
        cut.WaitForElement("[data-testid=ingredient]");

        cut.FindAll("[data-testid=ingredient] .mud-chip-close-button")[1].Click();
        cut.Find("input[data-testid=new-ingredient]").Input("Bacon");
        cut.Find("[data-testid=results] form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(["eggs", "bacon"], Session.Ingredients.Select(i => i.Name)));
        Assert.Equal(2, cut.FindAll("[data-testid=ingredient]").Count);
    }

    [Fact]
    public void Api_error_is_shown_and_results_stay_hidden()
    {
        Api.Throw = new ApiException("The photo is too large.");
        var cut = Render<ScanPage>();
        Upload(cut);

        cut.WaitForElement("[data-testid=analyse-button]:not([disabled])").Click();

        cut.WaitForAssertion(() => Assert.Equal("The photo is too large.", cut.Find("[data-testid=error]").TextContent.Trim()));
        Assert.Empty(cut.FindAll("[data-testid=results]"));
    }

    [Fact]
    public void Unreadable_photo_shows_error()
    {
        PhotoPreparer.Throw = new PhotoException("That file isn't a photo.");
        var cut = Render<ScanPage>();

        Upload(cut);

        cut.WaitForAssertion(() => Assert.Contains("isn't a photo", cut.Find("[data-testid=error]").TextContent));
        Assert.Empty(cut.FindAll("[data-testid=preview]"));
    }

    [Fact]
    public void Offline_shows_warning_and_disables_analysis()
    {
        var cut = Render<ScanPage>();
        Upload(cut);
        cut.WaitForElement("[data-testid=analyse-button]:not([disabled])");

        Connectivity.Set(false);

        cut.WaitForElement("[data-testid=offline-warning]");
        Assert.True(cut.Find("[data-testid=analyse-button]").HasAttribute("disabled"));
    }

    [Fact]
    public void Demo_mode_shows_banner()
    {
        Api.IsDemo = true;

        var cut = Render<ScanPage>();

        cut.Find("[data-testid=demo-banner]");
    }

    [Fact]
    public void Results_survive_leaving_and_returning_to_the_page()
    {
        var first = Render<ScanPage>();
        Upload(first);
        first.WaitForElement("[data-testid=analyse-button]:not([disabled])").Click();
        first.WaitForElement("[data-testid=ingredient]");
        first.Dispose();

        var second = Render<ScanPage>();

        Assert.Equal(2, second.FindAll("[data-testid=ingredient]").Count);
        second.Find("[data-testid=preview]");
    }

    private static void Upload(IRenderedComponent<ScanPage> cut) =>
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Photo, "fridge.jpg", contentType: "image/jpeg"));
}

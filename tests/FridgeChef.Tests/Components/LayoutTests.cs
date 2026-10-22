using FridgeChef.Layout;

namespace FridgeChef.Tests.Components;

public class LayoutTests : AppTestContext
{
    [Fact]
    public void StatusIndicator_shows_online_then_offline()
    {
        var cut = Render<StatusIndicator>();
        Assert.Contains("Online", cut.Find("[data-testid=status-indicator]").TextContent);

        Connectivity.Set(false);

        cut.WaitForAssertion(() => Assert.Contains("Offline", cut.Find("[data-testid=status-indicator]").TextContent));
    }

    [Fact]
    public void UpdatePrompt_hidden_until_update_available_then_applies()
    {
        var cut = Render<UpdatePrompt>();
        Assert.Empty(cut.FindAll("[data-testid=update-prompt]"));

        Updates.RaiseUpdateAvailable();

        cut.WaitForAssertion(() => Assert.Contains("New version available", cut.Find("[data-testid=update-prompt]").TextContent));
        cut.Find("[data-testid=update-prompt] button").Click();
        Assert.Equal(1, Updates.ApplyCount);
    }

    [Fact]
    public void Footer_shows_version()
    {
        var cut = Render<AppFooter>();

        Assert.Contains("v1.0.42", cut.Markup);
    }
}

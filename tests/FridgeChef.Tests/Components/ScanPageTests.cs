using FridgeChef.Features.Scan;

namespace FridgeChef.Tests.Components;

public class ScanPageTests : AppTestContext
{
    [Fact]
    public void Offline_shows_warning_and_disables_analysis()
    {
        var cut = Render<ScanPage>();
        Assert.Empty(cut.FindAll("[data-testid=offline-warning]"));

        Connectivity.Set(false);

        cut.WaitForAssertion(() => cut.Find("[data-testid=offline-warning]"));
        Assert.True(cut.Find("[data-testid=analyse-button]").HasAttribute("disabled"));
    }
}

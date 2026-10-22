using FridgeChef.Services;

namespace FridgeChef.Tests.Services;

public class AppInfoTests
{
    [Fact]
    public void Version_comes_from_assembly_without_build_metadata()
    {
        var version = new AppInfo().Version;

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain('+', version);
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }
}

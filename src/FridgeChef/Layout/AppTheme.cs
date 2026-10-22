using MudBlazor;

namespace FridgeChef.Layout;

public static class AppTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2e7d32",
            Secondary = "#ef6c00",
            AppbarBackground = "#2e7d32",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#66bb6a",
            Secondary = "#ffa726",
            AppbarBackground = "#1b1d22",
            Background = "#121317",
            Surface = "#1b1d22",
            DrawerBackground = "#1b1d22",
        },
    };
}

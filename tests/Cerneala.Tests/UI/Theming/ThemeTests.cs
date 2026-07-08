using Cerneala.Drawing;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Theming;

public sealed class ThemeTests
{
    [Fact]
    public void ThemeResolvesTypedValues()
    {
        ThemeKey<DrawColor> key = new("Accent");
        Theme theme = new Theme("Test").Set(key, DrawColor.White);

        Assert.True(theme.TryGet(key, out DrawColor value));
        Assert.Equal(DrawColor.White, value);
        Assert.Equal(DrawColor.White, theme.Get(key));
    }

    [Fact]
    public void MissingThemeValueFailsClearly()
    {
        Theme theme = new();
        ThemeKey<DrawColor> key = new("Missing");

        Assert.Throws<KeyNotFoundException>(() => theme.Get(key));
    }

    [Fact]
    public void DefaultThemeProvidesPalette()
    {
        Theme theme = DefaultTheme.Create();

        ThemePalette palette = theme.Get(DefaultTheme.PaletteKey);

        Assert.Equal(theme.Get(DefaultTheme.BackgroundKey), palette.Background);
        Assert.Equal(theme.Get(DefaultTheme.ForegroundKey), palette.Foreground);
    }

}

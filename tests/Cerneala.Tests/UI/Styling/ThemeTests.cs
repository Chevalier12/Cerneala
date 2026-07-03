using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

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

    [Fact]
    public void ThemeResourceSetterResolvesThroughProvider()
    {
        ThemeKey<DrawColor> key = new("Accent");
        ThemeProvider provider = new(new Theme().Set(key, DrawColor.White));
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(key))));

        new StyleApplicator().Apply(button, sheet, provider);

        Assert.Equal(DrawColor.White, button.Background);
    }

    [Fact]
    public void ThemeReplacementRecomputesTrackedValues()
    {
        ThemeKey<DrawColor> key = new("Accent");
        ThemeProvider provider = new(new Theme().Set(key, DrawColor.White));
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(key))));

        using StyleInvalidation invalidation = new(new StyleApplicator(), sheet, provider);
        invalidation.Track(button);
        provider.Theme = new Theme().Set(key, DrawColor.Black);

        Assert.Equal(DrawColor.Black, button.Background);
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class DefaultThemeVerticalSliceTests
{
    [Fact]
    public void DefaultThemeProvidesButtonTextAndSurfaceColors()
    {
        UIRoot root = StyledRoot(out ThemeProvider provider);
        Button button = new();
        TextBlock text = new() { Text = "hello" };
        Border surface = new();
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(text);
        root.VisualChildren.Add(surface);

        root.ProcessFrame();

        Assert.Equal(provider.Get(DefaultTheme.SurfaceKey), button.Background);
        Assert.Equal(provider.Get(DefaultTheme.ForegroundKey), button.Foreground);
        Assert.Equal(provider.Get(DefaultTheme.BorderKey), button.BorderColor);
        Assert.Equal(provider.Get(DefaultTheme.ForegroundKey), text.Foreground);
        Assert.Equal(provider.Get(DefaultTheme.SurfaceKey), surface.Background);
        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, text.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, surface.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void StyleSheetAppliesDefaultButtonVisualsWithoutLocalColors()
    {
        UIRoot root = StyledRoot(out _);
        Button button = new();
        root.VisualChildren.Add(button);

        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BorderColorProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void HoverPseudoClassUpdatesStyledButtonThroughScheduler()
    {
        UIRoot root = StyledRoot(out ThemeProvider provider);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.IsPointerOver = true;

        Assert.Contains(button, root.StyleQueue.Snapshot());
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(provider.Get(DefaultTheme.AccentKey), button.Background);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, button.GetValueSource(Control.BackgroundProperty));
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void KeyboardFocusPseudoClassUpdatesStyledButtonThroughScheduler()
    {
        UIRoot root = StyledRoot(out ThemeProvider provider);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.IsKeyboardFocused = true;

        Assert.Contains(button, root.StyleQueue.Snapshot());
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(provider.Get(DefaultTheme.AccentKey), button.BorderColor);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, button.GetValueSource(Control.BorderColorProperty));
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void ThemeChangeInvalidatesStyledControlsWithoutLayoutWhenOnlyColorsChange()
    {
        UIRoot root = StyledRoot(out ThemeProvider provider);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Theme changed = new Theme("Changed")
            .Set(DefaultTheme.PaletteKey, provider.Get(DefaultTheme.PaletteKey))
            .Set(DefaultTheme.BackgroundKey, new DrawColor(1, 2, 3))
            .Set(DefaultTheme.ForegroundKey, new DrawColor(4, 5, 6))
            .Set(DefaultTheme.SurfaceKey, new DrawColor(7, 8, 9))
            .Set(DefaultTheme.BorderKey, new DrawColor(10, 11, 12))
            .Set(DefaultTheme.AccentKey, new DrawColor(13, 14, 15));

        provider.Theme = changed;
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new DrawColor(7, 8, 9), button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.Equal(0, stats.MeasureCalls);
        Assert.Equal(0, stats.ArrangeCalls);
    }

    private static UIRoot StyledRoot(out ThemeProvider provider)
    {
        provider = new ThemeProvider(DefaultTheme.Create());
        UIRoot root = new(100, 100);
        root.SetThemeProvider(provider);
        root.SetStyleSheet(DefaultTheme.CreateStyleSheet());
        return root;
    }
}

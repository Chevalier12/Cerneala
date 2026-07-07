using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Styling;
using Cerneala.UI.Styling;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Styling;

public sealed class StyleMotionTests
{
    [Fact]
    public void DefaultThemeProvidesAllMotionTokens()
    {
        Theme theme = DefaultTheme.Create();

        MotionTokens tokens = theme.Get(ThemeMotionTokens.Key);

        Assert.NotNull(tokens.Get(ThemeMotionTokens.Instant));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.FastOut));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.FastIn));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.Standard));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.Emphasized));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.GentleSpring));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.SnappySpring));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.LayoutSpring));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.Enter));
        Assert.NotNull(tokens.Get(ThemeMotionTokens.Exit));
    }

    [Fact]
    public void VisualStateChangeUsesConfiguredMotionToken()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Theme theme = DefaultTheme.Create();
        root.SetThemeProvider(new ThemeProvider(theme));
        Button button = new();
        root.VisualChildren.Add(button);
        root.SetStyleSheet(new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)))
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges))));
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(60));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, button.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(DrawColor.Black, button.Background);
        Assert.NotEqual(DrawColor.White, button.Background);
    }

    [Fact]
    public void MissingMotionTokenThrowsClearException()
    {
        UIRoot root = new();
        root.SetThemeProvider(new ThemeProvider(new Theme().Set(ThemeMotionTokens.Key, new MotionTokens())));
        Button button = new();
        root.VisualChildren.Add(button);
        root.SetStyleSheet(new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, "Nope", StyleMotionScope.VisualStateChanges))));

        button.IsPointerOver = true;

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() => root.ProcessFrame());
        Assert.Contains("Nope", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MotionTokens), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeChangeAffectsFutureStyleMotionOnly()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Theme fast = new Theme().Set(ThemeMotionTokens.Key, new MotionTokens()
            .Set(ThemeMotionTokens.FastOut, MotionFactory.Tween(TimeSpan.FromMilliseconds(100))));
        Theme slow = new Theme().Set(ThemeMotionTokens.Key, new MotionTokens()
            .Set(ThemeMotionTokens.FastOut, MotionFactory.Tween(TimeSpan.FromMilliseconds(1000))));
        ThemeProvider provider = new(fast);
        root.SetThemeProvider(provider);
        Button button = new();
        root.VisualChildren.Add(button);
        root.SetStyleSheet(new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)))
            .Add(new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White))
                .AddMotion(new StyleMotion<DrawColor>(Control.BackgroundProperty, ThemeMotionTokens.FastOut, StyleMotionScope.VisualStateChanges))));
        root.ProcessFrame();

        button.IsPointerOver = true;
        root.ProcessFrame();
        provider.Theme = slow;
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        DrawColor completedFirst = button.GetValue(Control.BackgroundProperty);

        button.IsPointerOver = false;
        root.ProcessFrame();
        button.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(DrawColor.White, completedFirst);
        Assert.NotEqual(DrawColor.White, button.GetValue(Control.BackgroundProperty));
    }
}

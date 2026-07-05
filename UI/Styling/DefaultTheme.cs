using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Styling;

public static class DefaultTheme
{
    public static readonly ThemeKey<ThemePalette> PaletteKey = new("Palette");
    public static readonly ThemeKey<DrawColor> BackgroundKey = new("Background");
    public static readonly ThemeKey<DrawColor> ForegroundKey = new("Foreground");
    public static readonly ThemeKey<DrawColor> SurfaceKey = new("Surface");
    public static readonly ThemeKey<DrawColor> BorderKey = new("Border");
    public static readonly ThemeKey<DrawColor> AccentKey = new("Accent");

    public static Theme Create()
    {
        ThemePalette palette = new(
            new DrawColor(248, 250, 252),
            new DrawColor(28, 35, 48),
            new DrawColor(255, 255, 255),
            new DrawColor(148, 163, 184),
            new DrawColor(37, 99, 235));

        return new Theme("Default")
            .Set(PaletteKey, palette)
            .Set(BackgroundKey, palette.Background)
            .Set(ForegroundKey, palette.Foreground)
            .Set(SurfaceKey, palette.Surface)
            .Set(BorderKey, palette.Border)
            .Set(AccentKey, palette.Accent);
    }

    public static StyleSheet CreateStyleSheet()
    {
        return new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(SurfaceKey)))
                .Add(new Setter<DrawColor>(Control.ForegroundProperty, new ThemeResource<DrawColor>(ForegroundKey)))
                .Add(new Setter<DrawColor>(Control.BorderColorProperty, new ThemeResource<DrawColor>(BorderKey)))
                .Add(new Setter<Thickness>(Control.BorderThicknessProperty, new Thickness(1))))
            .Add(new StyleRule(
                    StyleSelector.ForType<Button>(),
                    new VisualStateRule(PseudoClass.Hover))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(AccentKey))))
            .Add(new StyleRule(
                    StyleSelector.ForType<Button>(),
                    new VisualStateRule(PseudoClass.Focus))
                .Add(new Setter<DrawColor>(Control.BorderColorProperty, new ThemeResource<DrawColor>(AccentKey))))
            .Add(new StyleRule(
                    StyleSelector.ForType<Button>(),
                    new VisualStateRule(PseudoClass.Pressed))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(BorderKey)))
                .Add(new Setter<DrawColor>(Control.ForegroundProperty, new ThemeResource<DrawColor>(SurfaceKey))))
            .Add(new StyleRule(
                    StyleSelector.ForType<Button>(),
                    new VisualStateRule(PseudoClass.Disabled))
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(BorderKey))))
            .Add(new StyleRule(StyleSelector.ForType<TextBlock>())
                .Add(new Setter<DrawColor>(Control.ForegroundProperty, new ThemeResource<DrawColor>(ForegroundKey))))
            .Add(new StyleRule(StyleSelector.ForType<Border>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, new ThemeResource<DrawColor>(SurfaceKey)))
                .Add(new Setter<DrawColor>(Control.BorderColorProperty, new ThemeResource<DrawColor>(BorderKey))));
    }
}

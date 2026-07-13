using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Media;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Aspect;

public sealed class ThemeTokenBridgeTests
{
    [Fact]
    public void ThemeKeyCanBeProjectedIntoAspectToken()
    {
        ThemeKey<Color> key = new("Accent");

        AspectToken<Color> token = ThemeTokenBridge.ToToken(key);

        Assert.Equal("theme.Accent", token.Name);
        Assert.Equal(typeof(Color), token.ValueType);
    }

    [Fact]
    public void DefaultThemeTokensMatchExistingDefaultThemeValues()
    {
        Theme theme = DefaultTheme.Create();
        AspectEnvironment environment = ThemeTokenBridge.CreateEnvironment(theme);

        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BackgroundKey), out Color background));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.ForegroundKey), out Color foreground));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.SurfaceKey), out Color surface));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BorderKey), out Color border));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.AccentKey), out Color accent));

        Assert.Equal(theme.Get(DefaultTheme.BackgroundKey), background);
        Assert.Equal(theme.Get(DefaultTheme.ForegroundKey), foreground);
        Assert.Equal(theme.Get(DefaultTheme.SurfaceKey), surface);
        Assert.Equal(theme.Get(DefaultTheme.BorderKey), border);
        Assert.Equal(theme.Get(DefaultTheme.AccentKey), accent);
    }

    [Fact]
    public void ThemeColorsAreProjectedIntoEverySemanticBrushToken()
    {
        Theme theme = DefaultTheme.Create();
        AspectEnvironment environment = ThemeTokenBridge.CreateEnvironment(theme);

        AssertBrush(environment, DefaultAspectTokens.Brush.Background, theme.Get(DefaultTheme.BackgroundKey));
        AssertBrush(environment, DefaultAspectTokens.Brush.Foreground, theme.Get(DefaultTheme.ForegroundKey));
        AssertBrush(environment, DefaultAspectTokens.Brush.Surface, theme.Get(DefaultTheme.SurfaceKey));
        AssertBrush(environment, DefaultAspectTokens.Brush.Border, theme.Get(DefaultTheme.BorderKey));
    }

    private static void AssertBrush(AspectEnvironment environment, AspectToken<Brush?> token, Color expected)
    {
        Assert.True(environment.TryGet(token, out Brush? brush));
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(brush).Color);
    }
}

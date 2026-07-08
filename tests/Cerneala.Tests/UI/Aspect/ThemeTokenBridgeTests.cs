using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Aspect;

public sealed class ThemeTokenBridgeTests
{
    [Fact]
    public void ThemeKeyCanBeProjectedIntoAspectToken()
    {
        ThemeKey<DrawColor> key = new("Accent");

        AspectToken<DrawColor> token = ThemeTokenBridge.ToToken(key);

        Assert.Equal("theme.Accent", token.Name);
        Assert.Equal(typeof(DrawColor), token.ValueType);
    }

    [Fact]
    public void DefaultThemeTokensMatchExistingDefaultThemeValues()
    {
        Theme theme = DefaultTheme.Create();
        AspectEnvironment environment = ThemeTokenBridge.CreateEnvironment(theme);

        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BackgroundKey), out DrawColor background));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.ForegroundKey), out DrawColor foreground));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.SurfaceKey), out DrawColor surface));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BorderKey), out DrawColor border));
        Assert.True(environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.AccentKey), out DrawColor accent));

        Assert.Equal(theme.Get(DefaultTheme.BackgroundKey), background);
        Assert.Equal(theme.Get(DefaultTheme.ForegroundKey), foreground);
        Assert.Equal(theme.Get(DefaultTheme.SurfaceKey), surface);
        Assert.Equal(theme.Get(DefaultTheme.BorderKey), border);
        Assert.Equal(theme.Get(DefaultTheme.AccentKey), accent);
    }
}

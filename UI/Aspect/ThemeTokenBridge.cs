using Cerneala.Drawing;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Theming;
using Cerneala.UI.Media;

namespace Cerneala.UI.Aspect;

public static class ThemeTokenBridge
{
    public static AspectToken<T> ToToken<T>(ThemeKey<T> key)
    {
        return AspectToken.Create<T>($"theme.{key.Key}");
    }

    public static AspectEnvironment CreateEnvironment(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        AspectEnvironment environment = new(theme.Name ?? "theme");
        Apply(theme, environment);
        return environment;
    }

    internal static void Apply(Theme theme, AspectEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(environment);

        Project(theme, environment, DefaultTheme.BackgroundKey);
        Project(theme, environment, DefaultTheme.ForegroundKey);
        Project(theme, environment, DefaultTheme.SurfaceKey);
        Project(theme, environment, DefaultTheme.BorderKey);
        Project(theme, environment, DefaultTheme.AccentKey);
        ProjectColorAndBrush(theme, environment, DefaultTheme.BackgroundKey, DefaultAspectTokens.Color.Background, DefaultAspectTokens.Brush.Background);
        ProjectColorAndBrush(theme, environment, DefaultTheme.ForegroundKey, DefaultAspectTokens.Color.Foreground, DefaultAspectTokens.Brush.Foreground);
        ProjectColorAndBrush(theme, environment, DefaultTheme.SurfaceKey, DefaultAspectTokens.Color.Surface, DefaultAspectTokens.Brush.Surface);
        ProjectColorAndBrush(theme, environment, DefaultTheme.BorderKey, DefaultAspectTokens.Color.Border, DefaultAspectTokens.Brush.Border);
        ProjectColor(theme, environment, DefaultTheme.AccentKey, DefaultAspectTokens.Color.Accent);

        ProjectBrush(theme, environment, DefaultTheme.SurfaceKey, ButtonTokens.Background);
        ProjectBrush(theme, environment, DefaultTheme.ForegroundKey, ButtonTokens.Foreground);
        ProjectBrush(theme, environment, DefaultTheme.BorderKey, ButtonTokens.BorderBrush);
        ProjectBrush(theme, environment, DefaultTheme.AccentKey, ButtonTokens.HoverBackground);
        ProjectBrush(theme, environment, DefaultTheme.BorderKey, ButtonTokens.PressedBackground);
    }

    private static void Project<T>(Theme theme, AspectEnvironment environment, ThemeKey<T> key)
    {
        if (theme.TryGet(key, out T value))
        {
            environment.Set(ToToken(key), value);
        }
    }

    private static void ProjectColor(
        Theme theme,
        AspectEnvironment environment,
        ThemeKey<Color> key,
        AspectToken<Color> token)
    {
        if (theme.TryGet(key, out Color color))
        {
            environment.Set(token, color);
        }
    }

    private static void ProjectColorAndBrush(
        Theme theme,
        AspectEnvironment environment,
        ThemeKey<Color> key,
        AspectToken<Color> colorToken,
        AspectToken<Brush?> brushToken)
    {
        if (theme.TryGet(key, out Color color))
        {
            environment.Set(colorToken, color);
            environment.Set(brushToken, new SolidColorBrush(color));
        }
    }

    private static void ProjectBrush(
        Theme theme,
        AspectEnvironment environment,
        ThemeKey<Color> key,
        AspectToken<Brush?> token)
    {
        if (theme.TryGet(key, out Color color))
        {
            environment.Set(token, new SolidColorBrush(color));
        }
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Theming;

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
        Project(theme, environment, DefaultTheme.BackgroundKey);
        Project(theme, environment, DefaultTheme.ForegroundKey);
        Project(theme, environment, DefaultTheme.SurfaceKey);
        Project(theme, environment, DefaultTheme.BorderKey);
        Project(theme, environment, DefaultTheme.AccentKey);
        return environment;
    }

    private static void Project<T>(Theme theme, AspectEnvironment environment, ThemeKey<T> key)
    {
        if (theme.TryGet(key, out T value))
        {
            environment.Set(ToToken(key), value);
        }
    }
}

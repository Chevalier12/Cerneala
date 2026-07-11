using Cerneala.Drawing;

namespace Cerneala.UI.Theming;

public sealed class ThemePalette
{
    public ThemePalette(
        Color background,
        Color foreground,
        Color surface,
        Color border,
        Color accent)
    {
        Background = background;
        Foreground = foreground;
        Surface = surface;
        Border = border;
        Accent = accent;
    }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color Surface { get; }

    public Color Border { get; }

    public Color Accent { get; }
}

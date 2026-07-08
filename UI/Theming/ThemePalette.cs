using Cerneala.Drawing;

namespace Cerneala.UI.Theming;

public sealed class ThemePalette
{
    public ThemePalette(
        DrawColor background,
        DrawColor foreground,
        DrawColor surface,
        DrawColor border,
        DrawColor accent)
    {
        Background = background;
        Foreground = foreground;
        Surface = surface;
        Border = border;
        Accent = accent;
    }

    public DrawColor Background { get; }

    public DrawColor Foreground { get; }

    public DrawColor Surface { get; }

    public DrawColor Border { get; }

    public DrawColor Accent { get; }
}

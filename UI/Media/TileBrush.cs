using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public abstract record TileBrush : Brush
{
    protected TileBrush(
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : base(opacity)
    {
        ValidateRect(viewport, nameof(viewport));
        ValidateRect(viewbox, nameof(viewbox));
        Stretch = stretch;
        AlignmentX = alignmentX;
        AlignmentY = alignmentY;
        Viewport = viewport;
        Viewbox = viewbox;
        TileMode = tileMode;
    }

    public DrawBrushStretch Stretch { get; }

    public DrawBrushAlignmentX AlignmentX { get; }

    public DrawBrushAlignmentY AlignmentY { get; }

    public DrawRect? Viewport { get; }

    public DrawRect? Viewbox { get; }

    public DrawTileMode TileMode { get; }

    private static void ValidateRect(DrawRect? rect, string parameterName)
    {
        if (rect is not DrawRect value)
        {
            return;
        }

        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Width) || !float.IsFinite(value.Height) ||
            value.Width <= 0 || value.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Tile rectangles must be finite and positive.");
        }
    }
}

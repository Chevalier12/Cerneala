using Cerneala.Drawing;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

// The ring between a control's outer shape and its inner shape, for a border that is not one uniform stroke: a side
// thickness or a corner radius differs. It is local to the arranged bounds, so it keeps its identity while the control
// only moves, and the render cache of a backend, which keys a path by identity, reuses the geometry it derived from it.
internal sealed class BorderOutlineCache
{
    private DrawPath? path;
    private (float Width, float Height, Thickness Thickness, CornerRadius CornerRadius) key;

    internal DrawPath Get(float width, float height, Thickness thickness, CornerRadius cornerRadius)
    {
        (float, float, Thickness, CornerRadius) next = (width, height, thickness, cornerRadius);
        if (path is null || key != next)
        {
            path = Build(width, height, thickness, cornerRadius);
            key = next;
        }

        return path;
    }

    // Two closed figures filled with the even-odd rule: the outer rounded rectangle, and the inner one that the
    // thicknesses leave, which is the hole. The inner corner radius is the outer one minus the thickness of the two
    // sides it joins, so the inner corners are elliptical when the two thicknesses differ. A side with thickness 0 puts
    // the inner edge on the outer edge, where the two coincide and the filled region is empty. The inner figure is
    // omitted when the thicknesses cover the whole rectangle.
    private static DrawPath Build(float width, float height, Thickness thickness, CornerRadius cornerRadius)
    {
        DrawCornerRadius outer = Border.ToDrawCornerRadius(cornerRadius, new DrawRect(0, 0, width, height));
        DrawPathBuilder builder = new();
        AddFigure(
            builder,
            0,
            0,
            width,
            height,
            new Corner(outer.TopLeft, outer.TopLeft),
            new Corner(outer.TopRight, outer.TopRight),
            new Corner(outer.BottomRight, outer.BottomRight),
            new Corner(outer.BottomLeft, outer.BottomLeft));

        float innerLeft = thickness.Left;
        float innerTop = thickness.Top;
        float innerRight = width - thickness.Right;
        float innerBottom = height - thickness.Bottom;
        float innerWidth = innerRight - innerLeft;
        float innerHeight = innerBottom - innerTop;
        if (innerWidth > 0 && innerHeight > 0)
        {
            Corner topLeft = new(MathF.Max(0, outer.TopLeft - thickness.Left), MathF.Max(0, outer.TopLeft - thickness.Top));
            Corner topRight = new(MathF.Max(0, outer.TopRight - thickness.Right), MathF.Max(0, outer.TopRight - thickness.Top));
            Corner bottomRight = new(MathF.Max(0, outer.BottomRight - thickness.Right), MathF.Max(0, outer.BottomRight - thickness.Bottom));
            Corner bottomLeft = new(MathF.Max(0, outer.BottomLeft - thickness.Left), MathF.Max(0, outer.BottomLeft - thickness.Bottom));
            float scale = MathF.Min(
                MathF.Min(Fit(innerWidth, topLeft.X + topRight.X), Fit(innerWidth, bottomLeft.X + bottomRight.X)),
                MathF.Min(Fit(innerHeight, topLeft.Y + bottomLeft.Y), Fit(innerHeight, topRight.Y + bottomRight.Y)));
            AddFigure(
                builder,
                innerLeft,
                innerTop,
                innerRight,
                innerBottom,
                topLeft.Scale(scale),
                topRight.Scale(scale),
                bottomRight.Scale(scale),
                bottomLeft.Scale(scale));
        }

        return builder.Build();
    }

    private static float Fit(float available, float requested)
    {
        return requested > available ? available / requested : 1;
    }

    // Clockwise from the end of the top-left corner. A corner whose radius is 0 on either axis is square.
    private static void AddFigure(
        DrawPathBuilder builder,
        float left,
        float top,
        float right,
        float bottom,
        Corner topLeft,
        Corner topRight,
        Corner bottomRight,
        Corner bottomLeft)
    {
        topLeft = topLeft.Resolve();
        topRight = topRight.Resolve();
        bottomRight = bottomRight.Resolve();
        bottomLeft = bottomLeft.Resolve();

        builder.MoveTo(new DrawPoint(left + topLeft.X, top));
        builder.LineTo(new DrawPoint(right - topRight.X, top));
        if (!topRight.IsSquare)
        {
            builder.ArcTo(topRight.X, topRight.Y, 0, false, true, new DrawPoint(right, top + topRight.Y));
        }

        builder.LineTo(new DrawPoint(right, bottom - bottomRight.Y));
        if (!bottomRight.IsSquare)
        {
            builder.ArcTo(bottomRight.X, bottomRight.Y, 0, false, true, new DrawPoint(right - bottomRight.X, bottom));
        }

        builder.LineTo(new DrawPoint(left + bottomLeft.X, bottom));
        if (!bottomLeft.IsSquare)
        {
            builder.ArcTo(bottomLeft.X, bottomLeft.Y, 0, false, true, new DrawPoint(left, bottom - bottomLeft.Y));
        }

        // Close() returns to the start, which is the end of the top-left corner, so a square top-left corner needs
        // no segment of its own.
        if (!topLeft.IsSquare)
        {
            builder.LineTo(new DrawPoint(left, top + topLeft.Y));
            builder.ArcTo(topLeft.X, topLeft.Y, 0, false, true, new DrawPoint(left + topLeft.X, top));
        }

        builder.Close();
    }

    private readonly record struct Corner(float X, float Y)
    {
        internal bool IsSquare => X <= 0 || Y <= 0;

        internal Corner Resolve() => IsSquare ? default : this;

        internal Corner Scale(float scale) => new(X * scale, Y * scale);
    }
}

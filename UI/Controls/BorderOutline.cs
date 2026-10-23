using Cerneala.Drawing;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

// The ring between a control's outer shape and its inner shape, for a border that is not one uniform stroke: a side
// thickness or a corner radius differs. A positive side thickness puts the outer edge that far outside the arranged
// bounds and a negative one puts the inner edge that far inside them. It is local to the top-left of the outer shape,
// so it keeps its identity while the control only moves, and the render cache of a backend, which keys a path by
// identity, reuses the geometry it derived from it.
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

    // Two closed figures filled with the even-odd rule: the outer figure, which is the bounds grown by each positive
    // side, and the inner figure, which is the bounds shrunk by each negative side and is the hole. The bounds are at
    // (outLeft, outTop) in the path. The corner radius r, scaled to fit the bounds, rounds the bounds. An outer corner
    // with r greater than 0 is elliptical with the radii r plus the positive side thickness of the two sides it joins,
    // so the ring keeps its width around it; with r equal to 0 it is square. The inner corner radius is r minus the
    // negative side thickness of the two sides it joins, so the inner corners are elliptical when the two differ. A side
    // with thickness 0 puts the inner edge on the outer edge, where the two coincide and the filled region is empty. The
    // inner figure is omitted when the negative thicknesses cover the whole bounds.
    private static DrawPath Build(float width, float height, Thickness thickness, CornerRadius cornerRadius)
    {
        DrawCornerRadius radius = Border.ToDrawCornerRadius(cornerRadius, new DrawRect(0, 0, width, height));
        float outLeft = MathF.Max(0, thickness.Left);
        float outTop = MathF.Max(0, thickness.Top);
        float outRight = MathF.Max(0, thickness.Right);
        float outBottom = MathF.Max(0, thickness.Bottom);
        float inLeft = MathF.Max(0, -thickness.Left);
        float inTop = MathF.Max(0, -thickness.Top);
        float inRight = MathF.Max(0, -thickness.Right);
        float inBottom = MathF.Max(0, -thickness.Bottom);

        DrawPathBuilder builder = new();
        AddFigure(
            builder,
            0,
            0,
            outLeft + width + outRight,
            outTop + height + outBottom,
            Outer(radius.TopLeft, outLeft, outTop),
            Outer(radius.TopRight, outRight, outTop),
            Outer(radius.BottomRight, outRight, outBottom),
            Outer(radius.BottomLeft, outLeft, outBottom));

        float innerLeft = outLeft + inLeft;
        float innerTop = outTop + inTop;
        float innerRight = outLeft + width - inRight;
        float innerBottom = outTop + height - inBottom;
        float innerWidth = innerRight - innerLeft;
        float innerHeight = innerBottom - innerTop;
        if (innerWidth > 0 && innerHeight > 0)
        {
            Corner topLeft = new(MathF.Max(0, radius.TopLeft - inLeft), MathF.Max(0, radius.TopLeft - inTop));
            Corner topRight = new(MathF.Max(0, radius.TopRight - inRight), MathF.Max(0, radius.TopRight - inTop));
            Corner bottomRight = new(MathF.Max(0, radius.BottomRight - inRight), MathF.Max(0, radius.BottomRight - inBottom));
            Corner bottomLeft = new(MathF.Max(0, radius.BottomLeft - inLeft), MathF.Max(0, radius.BottomLeft - inBottom));
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

    private static Corner Outer(float radius, float horizontalSide, float verticalSide)
    {
        return radius > 0 ? new Corner(radius + horizontalSide, radius + verticalSide) : default;
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

using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Rectangle : Shape
{
    public static readonly UiProperty<float> RadiusXProperty = UiProperty<float>.Register(
        nameof(RadiusX), typeof(Rectangle),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: IsValidRadius));

    public static readonly UiProperty<float> RadiusYProperty = UiProperty<float>.Register(
        nameof(RadiusY), typeof(Rectangle),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: IsValidRadius));

    private PathGeometry? roundedGeometry;
    private (float Width, float Height, float RadiusX, float RadiusY) roundedKey;

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float RadiusX
    {
        get => GetValue(RadiusXProperty);
        set => SetValue(RadiusXProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float RadiusY
    {
        get => GetValue(RadiusYProperty);
        set => SetValue(RadiusYProperty, value);
    }

    protected override Geometry ResolveGeometry(LayoutRect arrangedBounds)
    {
        if (Geometry is Geometry geometry)
        {
            return geometry;
        }

        DrawRect bounds = ToDrawRect(arrangedBounds);
        float radiusX = MathF.Min(RadiusX, bounds.Width / 2);
        float radiusY = MathF.Min(RadiusY, bounds.Height / 2);
        if (radiusX == 0 || radiusY == 0)
        {
            return new RectangleGeometry(bounds);
        }

        var key = (bounds.Width, bounds.Height, radiusX, radiusY);
        if (roundedGeometry is null || roundedKey != key)
        {
            float width = bounds.Width;
            float height = bounds.Height;
            roundedGeometry = PathGeometry.FromPath(new DrawPathBuilder()
                .MoveTo(new DrawPoint(radiusX, 0))
                .LineTo(new DrawPoint(width - radiusX, 0))
                .ArcTo(radiusX, radiusY, 0, false, true, new DrawPoint(width, radiusY))
                .LineTo(new DrawPoint(width, height - radiusY))
                .ArcTo(radiusX, radiusY, 0, false, true, new DrawPoint(width - radiusX, height))
                .LineTo(new DrawPoint(radiusX, height))
                .ArcTo(radiusX, radiusY, 0, false, true, new DrawPoint(0, height - radiusY))
                .LineTo(new DrawPoint(0, radiusY))
                .ArcTo(radiusX, radiusY, 0, false, true, new DrawPoint(radiusX, 0))
                .Close()
                .Build());
            roundedKey = key;
        }

        return roundedGeometry;
    }

    private static bool IsValidRadius(float value) => float.IsFinite(value) && value >= 0;
}

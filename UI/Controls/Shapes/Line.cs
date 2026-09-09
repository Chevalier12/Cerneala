using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Line : Shape
{
    public static readonly UiProperty<DrawPoint> StartPointProperty = UiProperty<DrawPoint>.Register(
        nameof(StartPoint), typeof(Line),
        new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> EndPointProperty = UiProperty<DrawPoint>.Register(
        nameof(EndPoint), typeof(Line),
        new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    private PathGeometry? lineGeometry;
    private DrawPoint cachedStart;
    private DrawPoint cachedEnd;

    public DrawPoint StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public DrawPoint EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    protected override Geometry ResolveGeometry(LayoutRect arrangedBounds)
    {
        if (Geometry is Geometry geometry)
        {
            return geometry;
        }

        DrawPoint start = StartPoint;
        DrawPoint end = EndPoint;
        if (lineGeometry is null || cachedStart != start || cachedEnd != end)
        {
            lineGeometry = new PathGeometry([start, end]);
            cachedStart = start;
            cachedEnd = end;
        }
        return lineGeometry;
    }
}

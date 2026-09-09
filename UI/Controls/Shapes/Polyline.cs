using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Polyline : Shape
{
    public static readonly UiProperty<IReadOnlyList<DrawPoint>> PointsProperty =
        PointShapeGeometry.RegisterPoints(typeof(Polyline));

    private readonly PointShapeGeometry pointGeometry = new(closed: false);

    public IReadOnlyList<DrawPoint> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    protected override Geometry? ResolveGeometry(LayoutRect arrangedBounds)
    {
        return Geometry ?? pointGeometry.Resolve(Points);
    }
}

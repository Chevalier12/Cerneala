using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

internal sealed class PointShapeGeometry(bool closed)
{
    private IReadOnlyList<DrawPoint>? cachedPoints;
    private PathGeometry? cachedGeometry;

    internal static UiProperty<IReadOnlyList<DrawPoint>> RegisterPoints(Type owner)
    {
        return UiProperty<IReadOnlyList<DrawPoint>>.Register(
            "Points", owner,
            new UiPropertyMetadata<IReadOnlyList<DrawPoint>>(Array.Empty<DrawPoint>(),
                UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
                coerceValue: static (_, points) =>
                {
                    ArgumentNullException.ThrowIfNull(points);
                    return Array.AsReadOnly(points.ToArray());
                }));
    }

    internal PathGeometry? Resolve(IReadOnlyList<DrawPoint> points)
    {
        if (!ReferenceEquals(cachedPoints, points))
        {
            cachedGeometry = points.Count < (closed ? 3 : 2)
                ? null
                : PathGeometry.FromPath(closed
                    ? DrawPathFactory.Polygon(points)
                    : DrawPathFactory.Polyline(points));
            cachedPoints = points;
        }
        return cachedGeometry;
    }
}

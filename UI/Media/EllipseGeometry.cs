using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record EllipseGeometry(DrawRect Rect) : Geometry
{
    public override DrawRect Bounds => Rect;
}

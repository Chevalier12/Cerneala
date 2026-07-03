using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record RectangleGeometry(DrawRect Rect) : Geometry
{
    public override DrawRect Bounds => Rect;
}

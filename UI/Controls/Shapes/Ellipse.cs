using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Ellipse : Shape
{
    protected override Geometry ResolveGeometry(LayoutRect arrangedBounds)
    {
        return Geometry ?? new EllipseGeometry(ToDrawRect(arrangedBounds));
    }
}

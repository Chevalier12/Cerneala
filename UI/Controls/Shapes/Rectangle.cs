using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class Rectangle : Shape
{
    protected override Geometry ResolveGeometry(LayoutRect arrangedBounds)
    {
        return Geometry ?? new RectangleGeometry(ToDrawRect(arrangedBounds));
    }
}

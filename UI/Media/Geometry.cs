using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public abstract record Geometry
{
    public abstract DrawRect Bounds { get; }
}

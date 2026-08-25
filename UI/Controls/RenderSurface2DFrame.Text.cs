using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class RenderSurface2DFrame
{
    public void DrawTextLayout(DrawTextLayout layout, DrawPoint origin)
    {
        EnsureActive();
        drawingContext.DrawTextLayout(layout, origin);
    }
}

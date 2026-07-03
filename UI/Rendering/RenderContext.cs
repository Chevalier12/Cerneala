using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Rendering;

public sealed class RenderContext
{
    public RenderContext(
        UIElement element,
        DrawingContext drawingContext,
        LayoutRect bounds,
        RenderLayer layer,
        RenderCounters counters)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        DrawingContext = drawingContext ?? throw new ArgumentNullException(nameof(drawingContext));
        Bounds = bounds;
        Layer = layer;
        Counters = counters ?? throw new ArgumentNullException(nameof(counters));
    }

    public UIElement Element { get; }

    public DrawingContext DrawingContext { get; }

    public LayoutRect Bounds { get; }

    public RenderLayer Layer { get; }

    public RenderCounters Counters { get; }
}

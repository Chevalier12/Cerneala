using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using System.Runtime.CompilerServices;

namespace Cerneala.UI.Media;

public sealed record VisualBrush : TileBrush
{
    [ThreadStatic]
    private static HashSet<UIElement>? captureStack;

    public VisualBrush(
        UIElement visual,
        DrawBrushStretch stretch = DrawBrushStretch.Fill,
        DrawBrushAlignmentX alignmentX = DrawBrushAlignmentX.Center,
        DrawBrushAlignmentY alignmentY = DrawBrushAlignmentY.Center,
        DrawRect? viewport = null,
        DrawRect? viewbox = null,
        DrawTileMode tileMode = DrawTileMode.None,
        float opacity = 1)
        : base(stretch, alignmentX, alignmentY, viewport, viewbox, tileMode, opacity)
    {
        Visual = visual ?? throw new ArgumentNullException(nameof(visual));
    }

    public UIElement Visual { get; }

    public override DrawBrushKind Kind => DrawBrushKind.Visual;

    public bool Equals(VisualBrush? other)
    {
        return ReferenceEquals(this, other) ||
            other is not null && base.Equals(other) && ReferenceEquals(Visual, other.Visual);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), RuntimeHelpers.GetHashCode(Visual));
    }

    protected override DrawBrushDescriptor CreateDescriptor()
    {
        HashSet<UIElement> active = captureStack ??= new HashSet<UIElement>(ReferenceEqualityComparer.Instance);
        if (!active.Add(Visual))
        {
            throw new InvalidOperationException("VisualBrush cycle detected while capturing its visual source.");
        }

        try
        {
            DrawCommandList commands = new();
            Capture(Visual, commands);
            LayoutRect bounds = Visual.ArrangedBounds;
            DrawRect contentBounds = new(bounds.X, bounds.Y, MathF.Max(1, bounds.Width), MathF.Max(1, bounds.Height));
            return new VisualDrawBrushDescriptor(
                Visual, commands.ToArray(), contentBounds,
                Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, Opacity);
        }
        finally
        {
            active.Remove(Visual);
        }
    }

    private static void Capture(UIElement element, DrawCommandList destination)
    {
        DrawCommandList local = new();
        DrawingContext context = new(local);
        element.Render(new RenderContext(element, context, element.ArrangedBounds, RenderLayer.Default, new RenderCounters()));
        foreach (DrawCommand command in local)
        {
            destination.Add(command);
        }

        foreach (UIElement child in element.VisualChildren)
        {
            Capture(child, destination);
        }
    }
}

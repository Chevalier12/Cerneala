using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class Border : Decorator
{
    protected override void OnRender(RenderContext context)
    {
        RenderBox(this, context);
    }

    internal static void RenderBox(Control control, RenderContext context)
    {
        DrawRect rect = ToDrawRect(context.Bounds);
        if (control.Background is { } background && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        Thickness borderThickness = control.BorderThickness;
        float thickness = MathF.Max(MathF.Max(borderThickness.Left, borderThickness.Top), MathF.Max(borderThickness.Right, borderThickness.Bottom));
        if (control.BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }
    }

    internal static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, MathF.Max(0, rect.Width), MathF.Max(0, rect.Height));
    }
}

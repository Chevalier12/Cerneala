using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class Border : Decorator
{
    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = ToDrawRect(context.Bounds);
        if (Background is { } background && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }
    }

    internal static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, MathF.Max(0, rect.Width), MathF.Max(0, rect.Height));
    }
}

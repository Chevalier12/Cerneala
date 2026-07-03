using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class ProgressBar : RangeBase
{
    public ProgressBar()
    {
        Background = new DrawColor(230, 230, 230);
        Foreground = new DrawColor(65, 135, 230);
        BorderColor = new DrawColor(120, 120, 120);
        BorderThickness = new Thickness(1);
    }

    public float ValueRatio => Maximum <= Minimum ? 0 : (Value - Minimum) / (Maximum - Minimum);

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(100, 12);
    }

    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, Background);
        }

        float fillWidth = rect.Width * MathF.Min(MathF.Max(ValueRatio, 0), 1);
        if (Foreground.A != 0 && fillWidth > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(new DrawRect(rect.X, rect.Y, fillWidth, rect.Height), Foreground);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderColor.A != 0 && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, BorderColor, thickness);
        }
    }
}

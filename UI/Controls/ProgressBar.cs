using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class ProgressBar : RangeBase
{
    public ProgressBar()
    {
        Background = new Cerneala.UI.Media.SolidColorBrush(new Color(230, 230, 230));
        Foreground = new Color(65, 135, 230);
        BorderBrush = new Cerneala.UI.Media.SolidColorBrush(new Color(120, 120, 120));
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
        if (Background is { } background && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        float fillWidth = rect.Width * MathF.Min(MathF.Max(ValueRatio, 0), 1);
        if (Foreground.A != 0 && fillWidth > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(new DrawRect(rect.X, rect.Y, fillWidth, rect.Height), Foreground);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }
    }
}

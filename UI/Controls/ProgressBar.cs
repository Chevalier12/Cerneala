using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

public class ProgressBar : RangeBase
{
    public ProgressBar()
    {
        SetFrameworkDefault(BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(new Color(230, 230, 230)));
        SetFrameworkDefault(ForegroundProperty, new SolidColorBrush(new Color(65, 135, 230)));
        SetFrameworkDefault(BorderBrushProperty, new Cerneala.UI.Media.SolidColorBrush(new Color(120, 120, 120)));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
    }

    // In double: for finite float endpoints such as -3e38 and 3e38, Maximum - Minimum overflows float to infinity.
    public float ValueRatio => Maximum <= Minimum ? 0 : (float)(((double)Value - Minimum) / ((double)Maximum - Minimum));

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
        if (Foreground is { } foreground && fillWidth > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(new DrawRect(rect.X, rect.Y, fillWidth, rect.Height), foreground);
        }

        Border.RenderBorder(this, context);
    }
}

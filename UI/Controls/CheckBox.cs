using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class CheckBox : ToggleButton
{
    private const float BoxSize = 14;
    private const float ContentGap = 6;

    public CheckBox()
    {
        BorderBrush = new Color(100, 110, 125);
        Foreground = new Color(35, 45, 60);
        Background = Color.Transparent;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        LayoutSize contentSize = Content is string text && !string.IsNullOrEmpty(text)
            ? new LayoutSize(text.Length * FontSize * 0.5f, FontSize)
            : base.MeasureCore(context);

        float width = BoxSize + (contentSize.Width > 0 ? ContentGap + contentSize.Width : 0);
        float height = MathF.Max(BoxSize, contentSize.Height);
        Thickness insets = Insets;
        return new LayoutSize(width + insets.Left + insets.Right, height + insets.Top + insets.Bottom);
    }

    protected override void OnRender(RenderContext context)
    {
        Thickness insets = Insets;
        float boxX = context.Bounds.X + insets.Left;
        float boxY = context.Bounds.Y + insets.Top + MathF.Max(0, (context.Bounds.Height - insets.Top - insets.Bottom - BoxSize) / 2);
        DrawRect box = new(boxX, boxY, BoxSize, BoxSize);

        Color boxFill = IsChecked ? Foreground : Background;
        if (boxFill.A != 0)
        {
            context.DrawingContext.FillRectangle(box, boxFill);
        }

        context.DrawingContext.DrawRectangle(box, BorderBrush, 1);

        if (IsChecked)
        {
            DrawRect mark = new(box.X + 3, box.Y + 3, box.Width - 6, box.Height - 6);
            context.DrawingContext.FillRectangle(mark, Color.White);
        }

        if (Content is string text && !string.IsNullOrEmpty(text))
        {
            DrawPoint point = new(box.X + BoxSize + ContentGap, context.Bounds.Y + insets.Top);
            context.DrawingContext.DrawText(new DrawTextRun(new ControlTextFont(FontFamily, FontSize), text, FontSize), point, Foreground);
        }
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public class Button : ButtonBase
{
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;

    public TextMeasurer TextMeasurer
    {
        get => textMeasurer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(textMeasurer, value))
            {
                return;
            }

            textMeasurer = value;
            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Button text measurer changed");
        }
    }

    public TextRenderer TextRenderer
    {
        get => textRenderer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(textRenderer, value))
            {
                return;
            }

            textRenderer = value;
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Render, "Button text renderer changed");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
        LayoutSize contentSize = Content is UIElement element
            ? element.Measure(new MeasureContext(available, context.Rounding))
            : MeasureTextContent(available);
        return ContentControl.Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        if (Content is UIElement element)
        {
            element.Arrange(new ArrangeContext(ContentControl.Deflate(context.FinalRect, Insets), context.Rounding));
        }

        return context.FinalRect;
    }

    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is not null)
        {
            return;
        }

        DrawColor background = ResolveBackground();
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderColor.A != 0 && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, BorderColor, thickness);
        }

        if (Content is string text && !string.IsNullOrEmpty(text))
        {
            LayoutRect contentBounds = ContentControl.Deflate(context.Bounds, Insets);
            DrawPoint point = new(contentBounds.X, contentBounds.Y);
            TextRenderer.Render(context.DrawingContext, text, CreateTextStyle(), contentBounds.Width, point, Foreground);
        }
    }

    private LayoutSize MeasureTextContent(LayoutSize availableSize)
    {
        return Content is string text
            ? TextMeasurer.Measure(text, CreateTextStyle(), availableSize.Width).Size
            : LayoutSize.Zero;
    }

    private TextRunStyle CreateTextStyle()
    {
        return new TextRunStyle(FontFamily, FontSize, color: Foreground);
    }

    private DrawColor ResolveBackground()
    {
        if (!IsEnabled)
        {
            return new DrawColor(160, 160, 160);
        }

        if (IsPressed)
        {
            return new DrawColor(120, 120, 120);
        }

        if (IsPointerOver || IsKeyboardFocused)
        {
            return new DrawColor(220, 220, 220);
        }

        return Background;
    }
}

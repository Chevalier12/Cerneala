using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Text;

namespace Cerneala.UI.Controls;

public class Button : ButtonBase
{
    private TextMeasurer textMeasurer = TextMeasurer.Default;
    private TextRenderer textRenderer = TextRenderer.Default;

    // As WPF's ButtonBase theme style, a Button centers its content inside its border and padding.
    public Button()
    {
        SetFrameworkDefault(HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
        SetFrameworkDefault(VerticalContentAlignmentProperty, VerticalAlignment.Center);
    }

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
            LayoutRect inner = ContentControl.Deflate(context.FinalRect, Insets);
            element.Arrange(new ArrangeContext(AlignContent(inner, element.DesiredSize), context.Rounding));
        }

        return context.FinalRect;
    }

    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is not null)
        {
            return;
        }

        Brush? background = ResolveBackground();
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (background is not null && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }

        if (Content is string text && !string.IsNullOrEmpty(text))
        {
            LayoutRect contentBounds = ContentControl.Deflate(context.Bounds, Insets);
            TextAspect aspect = CreateTextAspect();
            LayoutSize textSize = TextMeasurer.Measure(text, aspect, contentBounds.Width).Size;
            LayoutRect textBounds = AlignContent(contentBounds, textSize);
            DrawPoint point = new(textBounds.X, textBounds.Y);
            TextRenderer.Render(context.DrawingContext, text, aspect, contentBounds.Width, point);
        }
    }

    // The slot that a WPF ContentPresenter with HorizontalAlignment and VerticalAlignment bound to the content
    // alignment gets inside the inner rect: the content's size, or the whole inner size for Stretch.
    private LayoutRect AlignContent(LayoutRect inner, LayoutSize contentSize)
    {
        HorizontalAlignment horizontal = HorizontalContentAlignment;
        VerticalAlignment vertical = VerticalContentAlignment;
        float width = horizontal == HorizontalAlignment.Stretch ? inner.Width : MathF.Min(contentSize.Width, inner.Width);
        float height = vertical == VerticalAlignment.Stretch ? inner.Height : MathF.Min(contentSize.Height, inner.Height);
        float x = horizontal switch
        {
            HorizontalAlignment.Center => inner.X + ((inner.Width - width) / 2),
            HorizontalAlignment.Right => inner.X + inner.Width - width,
            _ => inner.X
        };
        float y = vertical switch
        {
            VerticalAlignment.Center => inner.Y + ((inner.Height - height) / 2),
            VerticalAlignment.Bottom => inner.Y + inner.Height - height,
            _ => inner.Y
        };

        return new LayoutRect(x, y, width, height);
    }

    private LayoutSize MeasureTextContent(LayoutSize availableSize)
    {
        return Content is string text
            ? TextMeasurer.Measure(text, CreateTextAspect(), availableSize.Width).Size
            : LayoutSize.Zero;
    }

    private TextAspect CreateTextAspect()
    {
        return new TextAspect(FontFamily, FontSize, foreground: Foreground);
    }

    private Brush? ResolveBackground()
    {
        if (GetValueSource(BackgroundProperty) != UiPropertyValueSource.Default)
        {
            return Background;
        }

        if (!IsEnabled)
        {
            return new SolidColorBrush(new Color(160, 160, 160));
        }

        if (IsPressed)
        {
            return new SolidColorBrush(new Color(120, 120, 120));
        }

        if (IsPointerOver || IsKeyboardFocused)
        {
            return new SolidColorBrush(new Color(220, 220, 220));
        }

        return Background;
    }
}

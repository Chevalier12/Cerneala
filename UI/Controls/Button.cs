using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
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

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(Button),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ContentControl.ContentEqualityComparer));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set
        {
            object? oldContent = Content;
            if (ReferenceEquals(oldContent, value) ||
                (oldContent is not UIElement && value is not UIElement && Equals(oldContent, value)))
            {
                SetValue(ContentProperty, value);
                return;
            }

            if (value is UIElement newElement)
            {
                ValidateCanAttachContentElement(newElement);
            }

            if (HostsContentDirectly)
            {
                RemoveContentElement(oldContent);
                SetValue(ContentProperty, value);
                AddContentElement(value);
                return;
            }

            SetValue(ContentProperty, value);
        }
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

    private UIElement? ContentElement => Content as UIElement;

    private bool HostsContentDirectly => Template is null;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = ContentControl.Deflate(context.AvailableSize, insets);
        LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(available, context.Rounding)) ??
            MeasureTextContent(available);
        return ContentControl.Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        ContentElement?.Arrange(new ArrangeContext(ContentControl.Deflate(context.FinalRect, Insets), context.Rounding));
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

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (!ReferenceEquals(args.Property, TemplateProperty))
        {
            base.OnPropertyChanged(args);
            return;
        }

        ReleaseContentElementFromOwnedSubtree();
        base.OnPropertyChanged(args);
        if (HostsContentDirectly)
        {
            AddContentElement(Content);
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

    private void AddContentElement(object? content)
    {
        if (content is not UIElement element)
        {
            return;
        }

        LogicalChildren.Add(element);
        VisualChildren.Add(element);
    }

    private void RemoveContentElement(object? content)
    {
        if (content is not UIElement element)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
    }

    private void ReleaseContentElementFromOwnedSubtree()
    {
        if (Content is UIElement element)
        {
            ContentControl.DetachChildFromOwnedSubtree(this, element);
        }
    }

    private void ValidateCanAttachContentElement(UIElement element)
    {
        if (ReferenceEquals(this, element))
        {
            throw new InvalidOperationException("An element cannot be assigned as content of itself.");
        }

        if (element.LogicalParent is not null || element.VisualParent is not null)
        {
            throw new InvalidOperationException("Element must be removed from its current parent before reparenting.");
        }

        if (Root is not null && element.Root is not null && !ReferenceEquals(Root, element.Root))
        {
            throw new InvalidOperationException("Element cannot be added under a different root.");
        }

        for (UIElement? current = LogicalParent; current is not null; current = current.LogicalParent)
        {
            if (ReferenceEquals(current, element))
            {
                throw new InvalidOperationException("An ancestor cannot be assigned as content.");
            }
        }

        for (UIElement? current = VisualParent; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, element))
            {
                throw new InvalidOperationException("An ancestor cannot be assigned as content.");
            }
        }
    }
}

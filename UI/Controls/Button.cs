using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class Button : ButtonBase
{
    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(Button),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

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

            RemoveContentElement(oldContent);
            SetValue(ContentProperty, value);
            AddContentElement(value);
        }
    }

    private UIElement? ContentElement => Content as UIElement;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Thickness insets = Insets;
        LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(ContentControl.Deflate(context.AvailableSize, insets), context.Rounding)) ??
            MeasureTextContent();
        return ContentControl.Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ContentElement?.Arrange(new ArrangeContext(ContentControl.Deflate(context.FinalRect, Insets), context.Rounding));
        return context.FinalRect;
    }

    protected override void OnRender(RenderContext context)
    {
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
            DrawPoint point = new(context.Bounds.X + Insets.Left, context.Bounds.Y + Insets.Top);
            context.DrawingContext.DrawText(new DrawTextRun(new ControlTextFont(FontFamily, FontSize), text, FontSize), point, Foreground);
        }
    }

    private LayoutSize MeasureTextContent()
    {
        return Content is string text
            ? new LayoutSize(text.Length * FontSize * 0.5f, FontSize)
            : LayoutSize.Zero;
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

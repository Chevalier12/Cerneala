using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class ContentControl : Control
{
    private static readonly IEqualityComparer<object?> ContentEqualityComparer = new ContentValueEqualityComparer();

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(ContentControl),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ContentEqualityComparer));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set
        {
            object? oldContent = Content;
            if (ContentEqualityComparer.Equals(oldContent, value))
            {
                SetValue(ContentProperty, value);
                return;
            }

            ValidateCanOwnChild(this, value as UIElement);
            RemoveContentElement(oldContent);
            try
            {
                AddContentElement(value);
                SetValue(ContentProperty, value);
            }
            catch
            {
                RemoveContentElement(value);
                AddContentElement(oldContent);
                SetValue(ContentProperty, oldContent);
                throw;
            }
        }
    }

    protected UIElement? ContentElement => Content as UIElement;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        Thickness insets = Insets;
        LayoutSize available = Deflate(context.AvailableSize, insets);
        LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(available, context.Rounding)) ?? LayoutSize.Zero;
        return Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        Thickness insets = Insets;
        LayoutRect inner = Deflate(context.FinalRect, insets);
        ContentElement?.Arrange(new ArrangeContext(inner, context.Rounding));
        return context.FinalRect;
    }

    internal static LayoutSize Deflate(LayoutSize size, Thickness thickness)
    {
        float width = size.IsWidthUnconstrained ? float.PositiveInfinity : MathF.Max(0, size.Width - thickness.Horizontal);
        float height = size.IsHeightUnconstrained ? float.PositiveInfinity : MathF.Max(0, size.Height - thickness.Vertical);
        return new LayoutSize(width, height);
    }

    internal static LayoutSize Inflate(LayoutSize size, Thickness thickness)
    {
        float width = size.IsWidthUnconstrained ? float.PositiveInfinity : size.Width + thickness.Horizontal;
        float height = size.IsHeightUnconstrained ? float.PositiveInfinity : size.Height + thickness.Vertical;
        return new LayoutSize(width, height);
    }

    internal static LayoutRect Deflate(LayoutRect rect, Thickness thickness)
    {
        return new LayoutRect(
            rect.X + thickness.Left,
            rect.Y + thickness.Top,
            MathF.Max(0, rect.Width - thickness.Horizontal),
            MathF.Max(0, rect.Height - thickness.Vertical));
    }

    internal static void ValidateCanOwnChild(UIElement owner, UIElement? child)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (child is null)
        {
            return;
        }

        if (ReferenceEquals(owner, child))
        {
            throw new InvalidOperationException("An element cannot be added as a child of itself.");
        }

        if (IsAncestor(owner, child, ElementChildRole.Logical) ||
            IsAncestor(owner, child, ElementChildRole.Visual))
        {
            throw new InvalidOperationException("An ancestor cannot be added as a child.");
        }

        if (child.LogicalParent is not null || child.VisualParent is not null)
        {
            throw new InvalidOperationException("Element must be removed from its current parent before reparenting.");
        }

        if (owner.Root is not null && child.Root is not null && !ReferenceEquals(owner.Root, child.Root))
        {
            throw new InvalidOperationException("Element cannot be added under a different root.");
        }
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

    private static bool IsAncestor(UIElement owner, UIElement candidate, ElementChildRole role)
    {
        UIElement? current = role == ElementChildRole.Logical
            ? owner.LogicalParent
            : owner.VisualParent;

        while (current is not null)
        {
            if (ReferenceEquals(current, candidate))
            {
                return true;
            }

            current = role == ElementChildRole.Logical
                ? current.LogicalParent
                : current.VisualParent;
        }

        return false;
    }

    private sealed class ContentValueEqualityComparer : IEqualityComparer<object?>
    {
        public new bool Equals(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is UIElement || right is UIElement)
            {
                return false;
            }

            return EqualityComparer<object?>.Default.Equals(left, right);
        }

        public int GetHashCode(object? value)
        {
            return value?.GetHashCode() ?? 0;
        }
    }
}

using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public sealed class StackPanel : Panel
{
    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(StackPanel),
        new UiPropertyMetadata<Orientation>(Orientation.Vertical, UiPropertyOptions.AffectsMeasure));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        float width = 0;
        float height = 0;
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            LayoutSize childSize = child.Measure(context);
            if (Orientation == Orientation.Vertical)
            {
                width = MathF.Max(width, childSize.Width);
                height += childSize.Height;
            }
            else
            {
                width += childSize.Width;
                height = MathF.Max(height, childSize.Height);
            }
        }

        return new LayoutSize(width, height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        float offset = 0;
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            if (Orientation == Orientation.Vertical)
            {
                child.Arrange(new ArrangeContext(
                    new LayoutRect(context.FinalRect.X, context.FinalRect.Y + offset, context.FinalRect.Width, child.DesiredSize.Height),
                    context.Rounding));
                offset += child.DesiredSize.Height;
            }
            else
            {
                child.Arrange(new ArrangeContext(
                    new LayoutRect(context.FinalRect.X + offset, context.FinalRect.Y, child.DesiredSize.Width, context.FinalRect.Height),
                    context.Rounding));
                offset += child.DesiredSize.Width;
            }
        }

        return context.FinalRect;
    }
}

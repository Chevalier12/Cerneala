using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public class StackPanel : Panel
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
        // With layout rounding every child desires whole device pixels, so the stacked length adds device pixels and
        // stays on a device pixel. Adding the logical sizes 57 / 1.75f and 1 / 1.75f instead gives a float one step
        // above 58 / 1.75f, which UIElement.Measure would round up to a 59th device pixel.
        LayoutRounding rounding = context.Rounding;
        float stackLength = 0;
        float width = 0;
        float height = 0;
        Orientation orientation = Orientation;
        LayoutSize childAvailableSize = orientation == Orientation.Vertical
            ? new LayoutSize(context.AvailableSize.Width, float.PositiveInfinity)
            : new LayoutSize(float.PositiveInfinity, context.AvailableSize.Height);
        MeasureContext childContext = new(childAvailableSize, context.Rounding);

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            LayoutSize childSize = child.Measure(childContext);
            if (orientation == Orientation.Vertical)
            {
                width = MathF.Max(width, childSize.Width);
                stackLength += rounding.ToDevicePixels(childSize.Height);
            }
            else
            {
                stackLength += rounding.ToDevicePixels(childSize.Width);
                height = MathF.Max(height, childSize.Height);
            }
        }

        float stackSize = rounding.FromDevicePixels(stackLength);
        return orientation == Orientation.Vertical ? new LayoutSize(width, stackSize) : new LayoutSize(stackSize, height);
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

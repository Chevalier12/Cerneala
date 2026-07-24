using Cerneala.UI.Core;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls.Primitives;

public sealed class TrackLayoutPanel : Cerneala.UI.Layout.Panels.Panel
{
    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(TrackLayoutPanel),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        LayoutSize childSize = base.MeasureCore(context);
        return Orientation == Orientation.Horizontal
            ? new LayoutSize(MathF.Max(32, childSize.Width), MathF.Max(10, childSize.Height))
            : new LayoutSize(MathF.Max(10, childSize.Width), MathF.Max(32, childSize.Height));
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }
}

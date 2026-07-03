namespace Cerneala.UI.Layout;

public readonly record struct LayoutResult(
    LayoutSize DesiredSize,
    LayoutRect ArrangedBounds,
    bool UsedMeasureCache,
    bool UsedArrangeCache,
    bool BoundsChanged);

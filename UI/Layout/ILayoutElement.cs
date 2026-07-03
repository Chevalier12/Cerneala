namespace Cerneala.UI.Layout;

public interface ILayoutElement
{
    LayoutSize DesiredSize { get; }

    LayoutRect ArrangedBounds { get; }

    int LayoutVersion { get; }

    LayoutSize Measure(MeasureContext context);

    LayoutRect Arrange(ArrangeContext context);
}

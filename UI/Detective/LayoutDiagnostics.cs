using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Detective;

public static class LayoutDiagnostics
{
    public static LayoutDiagnosticsSnapshot Capture(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new LayoutDiagnosticsSnapshot(
            element.GetType().Name,
            element.DesiredSize,
            element.ArrangedBounds,
            element.LayoutVersion,
            element.Visibility,
            element.LastMeasureAvailableSize,
            element.LastMeasureLayoutVersion,
            element.LastArrangeFinalRect,
            element.LastArrangeLayoutVersion);
    }
}

public sealed record LayoutDiagnosticsSnapshot(
    string ElementType,
    LayoutSize DesiredSize,
    LayoutRect ArrangedBounds,
    int LayoutVersion,
    Visibility Visibility,
    LayoutSize? LastMeasureAvailableSize,
    int LastMeasureLayoutVersion,
    LayoutRect? LastArrangeFinalRect,
    int LastArrangeLayoutVersion)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ElementType} desired={DetectiveInvariantText.Format(DesiredSize)} arranged={DetectiveInvariantText.Format(ArrangedBounds)} layoutVersion={LayoutVersion} visibility={Visibility}");
    }
}

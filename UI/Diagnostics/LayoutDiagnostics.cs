using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Diagnostics;

public static class LayoutDiagnostics
{
    public static LayoutDiagnosticsSnapshot Capture(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new LayoutDiagnosticsSnapshot(
            element.ElementId?.ToString(),
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
    string? ElementId,
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
            $"{ElementType}#{ElementId ?? "unattached"} desired={DesiredSize} arranged={ArrangedBounds} layoutVersion={LayoutVersion} visibility={Visibility}");
    }
}

using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout;

public static class LayoutBoundary
{
    public static bool IsBoundary(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.IsLayoutBoundary;
    }

    public static void SetIsBoundary(UIElement element, bool isBoundary)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.IsLayoutBoundary = isBoundary;
    }
}

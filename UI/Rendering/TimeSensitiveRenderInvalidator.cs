using Cerneala.UI.Elements;

namespace Cerneala.UI.Rendering;

public static class TimeSensitiveRenderInvalidator
{
    public static void Invalidate(UIElement root, TimeSpan frameTime)
    {
        ArgumentNullException.ThrowIfNull(root);
        Traverse(root, frameTime);
    }

    private static void Traverse(UIElement element, TimeSpan frameTime)
    {
        if (element is ITimeSensitiveRenderElement timeSensitive)
        {
            _ = timeSensitive.UpdateRenderTime(frameTime);
        }

        UIElementCollection children = element.VisualChildren.Count > 0
            ? element.VisualChildren
            : element.LogicalChildren;

        foreach (UIElement child in children)
        {
            Traverse(child, frameTime);
        }
    }
}

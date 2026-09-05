using Cerneala.UI.Controls;
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

        for (int index = 0; index < children.Count; index++)
        {
            // The surface owns temporal traversal of its retained scene. Overlay
            // UI children still use the ordinary UI clock traversal.
            if (element is RenderSurface2D surface && ReferenceEquals(children[index], surface.Scene))
            {
                continue;
            }
            Traverse(children[index], frameTime);
        }
    }
}

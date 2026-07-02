using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputRouteBuilder
{
    public ElementInputRouteMap Build(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        ElementInputRouteMap map = new();
        AddElementAndDescendants(root, null, map);
        return map;
    }

    private static void AddElementAndDescendants(
        UIElement element,
        UiElementId? nearestIncludedParentId,
        ElementInputRouteMap map)
    {
        UiElementId? parentForDescendants = nearestIncludedParentId;

        if (element.IsAttached && element.ElementId.HasValue && ShouldInclude(element))
        {
            UiElementId id = element.ElementId.Value;
            map.Add(element, id, nearestIncludedParentId);
            parentForDescendants = id;

            foreach ((RoutedEvent routedEvent, RoutedEventHandler handler) in element.Handlers.EnumerateHandlers())
            {
                map.InputTree.AddHandler(id, routedEvent, handler);
            }
        }

        foreach (UIElement child in element.VisualChildren)
        {
            AddElementAndDescendants(child, parentForDescendants, map);
        }
    }

    private static bool ShouldInclude(UIElement element)
    {
        return element.IsEnabled && element.IsVisible;
    }
}

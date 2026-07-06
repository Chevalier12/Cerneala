using Cerneala.UI.Elements;
namespace Cerneala.UI.Input;

public sealed class ElementInputRouteBuilder
{
    public ElementInputRouteMap Build(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        ElementInputRouteMap map = new();
        AddElementAndDescendants(root, null, map, includeDisabled: false);
        return map;
    }

    public ElementInputRouteMap BuildForCommandState(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        ElementInputRouteMap map = new();
        AddElementAndDescendants(root, null, map, includeDisabled: true);
        return map;
    }

    private static void AddElementAndDescendants(
        UIElement element,
        UiElementId? nearestIncludedParentId,
        ElementInputRouteMap map,
        bool includeDisabled)
    {
        UiElementId? parentForDescendants = nearestIncludedParentId;

        if (!UIElementVisibility.ParticipatesInInput(element))
        {
            return;
        }

        if (element.IsAttached && element.ElementId.HasValue && ShouldInclude(element, includeDisabled))
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
            AddElementAndDescendants(child, parentForDescendants, map, includeDisabled);
        }
    }

    private static bool ShouldInclude(UIElement element, bool includeDisabled)
    {
        return (includeDisabled || element.IsEnabled) && UIElementVisibility.ParticipatesInInput(element);
    }
}

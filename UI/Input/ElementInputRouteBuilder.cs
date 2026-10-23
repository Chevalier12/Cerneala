using Cerneala.UI.Elements;
namespace Cerneala.UI.Input;

public sealed class ElementInputRouteBuilder
{
    public ElementInputRouteMap Build(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return new RouteBuild(includeDisabled: false).Run(root);
    }

    public ElementInputRouteMap BuildForCommandState(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return new RouteBuild(includeDisabled: true).Run(root);
    }

    private static bool ShouldInclude(UIElement element, bool includeDisabled)
    {
        return (includeDisabled || element.IsEnabled) && UIElementVisibility.ParticipatesInInput(element);
    }

    // Projected content is a visual child of the root-owned layer, but routes like WPF Popup content: from its visual
    // root to the projection owner, then along the owner's ancestors. Traversal order stays visual, in the drawing order
    // of each element's visual children (Panel.ZIndex), and so does route order (and with it keyboard navigation order).
    private sealed class RouteBuild(bool includeDisabled)
    {
        private readonly ElementInputRouteMap map = new();
        private readonly Dictionary<IInputProjectionOwner, UiElementId?> ownerRouteIds = new(ReferenceEqualityComparer.Instance);
        private readonly List<PendingProjection> pending = [];

        public ElementInputRouteMap Run(UIRoot root)
        {
            AddElementAndDescendants(root, null, null);
            AddPendingProjections();
            return map;
        }

        private void AddElementAndDescendants(UIElement element, UiElementId? routeParentId, UiElementId? visualParentId)
        {
            if (!UIElementVisibility.ParticipatesInInput(element))
            {
                return;
            }

            if (element.LogicalParent is IInputProjectionOwner owner &&
                ReferenceEquals(owner.ProjectedInputRoot, element))
            {
                if (!ownerRouteIds.TryGetValue(owner, out UiElementId? ownerRouteId))
                {
                    // The owner is not registered yet; the projection is added after the rest of the tree.
                    pending.Add(new PendingProjection(element, owner, visualParentId));
                    return;
                }

                routeParentId = ownerRouteId;
            }

            AddCore(element, routeParentId, visualParentId);
        }

        private void AddCore(UIElement element, UiElementId? routeParentId, UiElementId? visualParentId)
        {
            UiElementId? routeParentForDescendants = routeParentId;
            UiElementId? visualParentForDescendants = visualParentId;

            if (element.IsAttached && element.ElementId.HasValue && ShouldInclude(element, includeDisabled))
            {
                UiElementId id = element.ElementId.Value;
                map.Add(element, id, routeParentId, visualParentId);
                routeParentForDescendants = id;
                visualParentForDescendants = id;

                foreach ((RoutedEvent routedEvent, RoutedEventHandler handler, bool handledEventsToo) in element.Handlers.EnumerateHandlers())
                {
                    map.InputTree.AddHandler(id, routedEvent, handler, handledEventsToo);
                }
            }

            if (element is IInputProjectionOwner owner)
            {
                ownerRouteIds[owner] = routeParentForDescendants;
            }

            UIElementCollection visualChildren = element.VisualChildren;
            for (int index = 0; index < visualChildren.Count; index++)
            {
                AddElementAndDescendants(
                    element.GetVisualChildInZOrder(index),
                    routeParentForDescendants,
                    visualParentForDescendants);
            }

            if (element is IInputSubtreeHost inputSubtreeHost)
            {
                foreach (UIElement child in inputSubtreeHost.GetInputSubtreeChildren())
                {
                    AddElementAndDescendants(
                        child,
                        routeParentForDescendants,
                        visualParentForDescendants);
                }
            }
        }

        private void AddPendingProjections()
        {
            while (pending.Count > 0)
            {
                int index = pending.FindIndex(projection => ownerRouteIds.ContainsKey(projection.Owner));
                if (index >= 0)
                {
                    PendingProjection projection = pending[index];
                    pending.RemoveAt(index);
                    AddCore(projection.Root, ownerRouteIds[projection.Owner], projection.VisualParentId);
                    continue;
                }

                // The owner is outside the input route, for example under a collapsed ancestor: the content keeps
                // routing through its visual ancestors.
                PendingProjection orphan = pending[0];
                pending.RemoveAt(0);
                AddCore(orphan.Root, orphan.VisualParentId, orphan.VisualParentId);
            }
        }

        private readonly record struct PendingProjection(UIElement Root, IInputProjectionOwner Owner, UiElementId? VisualParentId);
    }
}

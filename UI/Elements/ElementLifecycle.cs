namespace Cerneala.UI.Elements;

public static class ElementLifecycle
{
    public static void AttachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);
        root.Relay.VerifyAccess();
        ValidateSubtreeAttachment(root, element);

        AttachPreOrder(root, element, ElementChildRole.Logical);
        AttachPreOrder(root, element, ElementChildRole.Visual);
    }

    public static void DetachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);
        root.Relay.VerifyAccess();

        HashSet<UIElement> detached = new(ReferenceEqualityComparer.Instance);
        DetachPostOrder(root, element, ElementChildRole.Visual, detached);
        DetachPostOrder(root, element, ElementChildRole.Logical, detached);
    }

    internal static void ValidateSubtreeAttachment(UIRoot root, UIElement element)
    {
        HashSet<UIElement> validated = new(ReferenceEqualityComparer.Instance);
        ValidatePreOrder(root, element, ElementChildRole.Logical, validated);
        ValidatePreOrder(root, element, ElementChildRole.Visual, validated);
    }

    private static void ValidatePreOrder(
        UIRoot root,
        UIElement current,
        ElementChildRole role,
        HashSet<UIElement> validated)
    {
        if (validated.Add(current) && !ReferenceEquals(current.Root, root))
        {
            if (current.Root is not null)
            {
                throw new InvalidOperationException("Element is already attached to a different root.");
            }

            current.ValidateLifecycleRoot(root);
        }

        IReadOnlyList<UIElement> children = Children(current, role);
        for (int index = 0; index < children.Count; index++)
        {
            ValidatePreOrder(root, children[index], role, validated);
        }
    }

    private static void AttachPreOrder(UIRoot root, UIElement current, ElementChildRole role)
    {
        AttachSingle(root, current);
        IReadOnlyList<UIElement> children = Children(current, role);
        for (int index = 0; index < children.Count; index++)
        {
            AttachPreOrder(root, children[index], role);
        }
    }

    private static void DetachPostOrder(
        UIRoot root,
        UIElement current,
        ElementChildRole role,
        HashSet<UIElement> detached)
    {
        IReadOnlyList<UIElement> children = Children(current, role);
        for (int index = 0; index < children.Count; index++)
        {
            DetachPostOrder(root, children[index], role, detached);
        }

        DetachSingle(root, current, detached);
    }

    private static IReadOnlyList<UIElement> Children(UIElement element, ElementChildRole role)
    {
        return role == ElementChildRole.Logical
            ? element.LogicalChildren
            : element.VisualChildren;
    }

    private static void AttachSingle(UIRoot root, UIElement element)
    {
        if (ReferenceEquals(element.Root, root))
        {
            return;
        }

        if (element.Root is not null)
        {
            throw new InvalidOperationException("Element is already attached to a different root.");
        }

        element.AttachToRoot(root, root.ElementIds.GetOrCreate(element));
    }

    private static void DetachSingle(UIRoot root, UIElement element, HashSet<UIElement> detached)
    {
        if (!detached.Add(element) || !ReferenceEquals(element.Root, root))
        {
            return;
        }

        root.ElementIds.Release(element);
        root.ResourceDependencyTracker.RemoveOwner(element);
        element.DetachFromRoot();
        root.Motion.Properties.RemoveBindings(element);
        root.RemovePendingWork(element);
        root.AspectProcessor.Clear(element);
    }
}

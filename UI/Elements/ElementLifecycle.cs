namespace Cerneala.UI.Elements;

public static class ElementLifecycle
{
    public static void AttachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Logical))
        {
            AttachSingle(root, current);
        }

        foreach (UIElement current in ElementTreeWalker.PreOrder(element, ElementChildRole.Visual))
        {
            AttachSingle(root, current);
        }
    }

    public static void DetachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);

        HashSet<UIElement> detached = new(ReferenceEqualityComparer.Instance);
        foreach (UIElement current in ElementTreeWalker.PostOrder(element, ElementChildRole.Visual))
        {
            DetachSingle(root, current, detached);
        }

        foreach (UIElement current in ElementTreeWalker.PostOrder(element, ElementChildRole.Logical))
        {
            DetachSingle(root, current, detached);
        }
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
        root.AspectProcessor.Clear(element);
    }
}

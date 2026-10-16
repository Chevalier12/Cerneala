using System.Runtime.ExceptionServices;

namespace Cerneala.UI.Elements;

public static class ElementLifecycle
{
    public static void AttachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);
        root.Relay.VerifyAccess();
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        ValidatePreOrder(root, element, visited);
        visited.Clear();
        AttachPreOrder(root, element, visited);
    }

    public static void DetachSubtree(UIRoot root, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(element);
        root.Relay.VerifyAccess();

        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        ExceptionDispatchInfo? failure = null;
        DetachPostOrder(root, element, visited, ref failure);
        failure?.Throw();
    }

    internal static void ValidateSubtreeAttachment(UIRoot root, UIElement element)
    {
        HashSet<UIElement> validated = new(ReferenceEqualityComparer.Instance);
        ValidatePreOrder(root, element, validated);
    }

    private static void ValidatePreOrder(
        UIRoot root,
        UIElement current,
        HashSet<UIElement> validated)
    {
        if (!validated.Add(current)) { return; }
        if (!ReferenceEquals(current.Root, root))
        {
            if (current.Root is not null)
            {
                throw new InvalidOperationException("Element is already attached to a different root.");
            }

            current.ValidateLifecycleRoot(root);
        }

        for (int role = 0; role < 2; role++)
        {
            IReadOnlyList<UIElement> children = role == 0 ? current.LogicalChildren : current.VisualChildren;
            for (int index = 0; index < children.Count; index++)
            {
                ValidatePreOrder(root, children[index], validated);
            }
        }
    }

    private static void AttachPreOrder(UIRoot root, UIElement current, HashSet<UIElement> visited)
    {
        if (!visited.Add(current)) { return; }
        AttachSingle(root, current);
        for (int role = 0; role < 2; role++)
        {
            IReadOnlyList<UIElement> children = role == 0 ? current.LogicalChildren : current.VisualChildren;
            for (int index = 0; index < children.Count; index++)
            {
                AttachPreOrder(root, children[index], visited);
            }
        }
    }

    // A failing detach callback does not stop the traversal: every element is detached, then the first
    // failure propagates.
    private static void DetachPostOrder(
        UIRoot root,
        UIElement current,
        HashSet<UIElement> visited,
        ref ExceptionDispatchInfo? failure)
    {
        if (!visited.Add(current)) { return; }
        for (int role = 0; role < 2; role++)
        {
            IReadOnlyList<UIElement> children = role == 0 ? current.VisualChildren : current.LogicalChildren;
            for (int index = 0; index < children.Count; index++)
            {
                DetachPostOrder(root, children[index], visited, ref failure);
            }
        }

        try
        {
            DetachSingle(root, current);
        }
        catch (Exception exception)
        {
            failure ??= ExceptionDispatchInfo.Capture(exception);
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

    private static void DetachSingle(UIRoot root, UIElement element)
    {
        if (!ReferenceEquals(element.Root, root))
        {
            return;
        }

        root.ElementIds.Release(element);
        root.ResourceDependencyTracker.RemoveOwner(element);
        try
        {
            element.DetachFromRoot();
        }
        finally
        {
            root.Motion.Properties.RemoveBindings(element);
            root.RemovePendingWork(element);
            root.AspectProcessor.Clear(element);
        }
    }
}

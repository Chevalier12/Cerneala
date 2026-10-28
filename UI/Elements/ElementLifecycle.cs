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

    // A Loaded handler can change the tree during the walk (#886). The walk reads the live child lists and starts a
    // list again from its first child when a handler changed it, so a removal cannot make it skip a sibling; children
    // already visited return at once. It stops descending into an element that a handler detached, so the children
    // of a removed element are not attached.
    private static void AttachPreOrder(UIRoot root, UIElement current, HashSet<UIElement> visited)
    {
        if (!visited.Add(current)) { return; }
        AttachSingle(root, current);
        for (int role = 0; role < 2; role++)
        {
            UIElementCollection children = role == 0 ? current.LogicalChildren : current.VisualChildren;
            for (int index = 0; index < children.Count && ReferenceEquals(current.Root, root); index++)
            {
                int version = children.Version;
                AttachPreOrder(root, children[index], visited);
                if (children.Version != version) { index = -1; }
            }
        }
    }

    // A failing detach callback does not stop the traversal: every element is detached, then the first
    // failure propagates. An Unloaded handler can change the tree during the walk (#886): a changed child list is
    // walked again from its first child, so a removal cannot make the walk skip a sibling.
    private static void DetachPostOrder(
        UIRoot root,
        UIElement current,
        HashSet<UIElement> visited,
        ref ExceptionDispatchInfo? failure)
    {
        if (!visited.Add(current)) { return; }
        for (int role = 0; role < 2; role++)
        {
            UIElementCollection children = role == 0 ? current.VisualChildren : current.LogicalChildren;
            for (int index = 0; index < children.Count; index++)
            {
                int version = children.Version;
                DetachPostOrder(root, children[index], visited, ref failure);
                if (children.Version != version) { index = -1; }
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

        element.AttachToRoot(root);
    }

    private static void DetachSingle(UIRoot root, UIElement element)
    {
        // An element whose own Unloaded handler removes it is still detaching: the removal's detach skips it, and the
        // detach in progress finishes its cleanup once (#886).
        if (!ReferenceEquals(element.Root, root) || element.IsDetachingFromRoot)
        {
            return;
        }

        root.ResourceDependencyTracker.RemoveOwner(element);

        // Every cleanup step runs even when an earlier one throws, and the first failure propagates. A throw from a
        // finally block would replace the exception in flight, for example an Unloaded handler's exception by a
        // behavior lifetime's Dispose failure.
        ExceptionDispatchInfo? failure = null;
        try { element.DetachFromRoot(); }
        catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }

        try { root.Motion.Properties.RemoveBindings(element); }
        catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }

        try { root.RemovePendingWork(element); }
        catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }

        try { root.AspectProcessor.Clear(element); }
        catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }

        failure?.Throw();
    }
}

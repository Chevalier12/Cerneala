using System.Collections;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Elements;

public sealed class UIElementCollection : IReadOnlyList<UIElement>
{
    private readonly UIElement owner;
    private readonly ElementChildRole role;
    private readonly List<UIElement> children = [];

    internal UIElementCollection(UIElement owner, ElementChildRole role)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.role = role;
    }

    public event EventHandler<ElementTreeChange>? Changed;

    public int Count => children.Count;

    public UIElement this[int index] => children[index];

    public void Add(UIElement child)
    {
        ArgumentNullException.ThrowIfNull(child);
        owner.Root?.Motion.Presence.TryCancelExitForAdd(owner, child);

        if (ReferenceEquals(owner, child))
        {
            throw new InvalidOperationException("An element cannot be added as a child of itself.");
        }

        if (IsAncestor(child))
        {
            throw new InvalidOperationException("An ancestor cannot be added as a child.");
        }

        if (ContainsReference(child))
        {
            throw new InvalidOperationException("Element is already a child of this collection.");
        }

        UIElement? currentParent = GetParent(child);
        if (currentParent is not null)
        {
            throw new InvalidOperationException("Element must be removed from its current parent before reparenting.");
        }

        if (owner.Root is null && child.Root is not null)
        {
            throw new InvalidOperationException("Attached element cannot be added under a detached parent.");
        }

        if (owner.Root is not null && child.Root is not null && !ReferenceEquals(owner.Root, child.Root))
        {
            throw new InvalidOperationException("Element cannot be added under a different root.");
        }

        children.Add(child);
        SetParent(child, owner);

        UIRoot? root = owner.Root;
        if (root is not null)
        {
            ElementLifecycle.AttachSubtree(root, child);
            root.IncrementTreeVersion();
        }

        InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Added);
        Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Added));
    }

    public bool Remove(UIElement child)
    {
        ArgumentNullException.ThrowIfNull(child);

        int index = IndexOfReference(child);
        if (index < 0)
        {
            return false;
        }

        if (role == ElementChildRole.Visual && owner.Root?.Motion.Presence.TryBeginExit(owner, child) == true)
        {
            children.RemoveAt(index);
            SetParent(child, null);
            owner.Root.IncrementTreeVersion();
            InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Removed);
            Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
            return true;
        }

        children.RemoveAt(index);

        UIRoot? oldRoot = child.Root;
        SetParent(child, null);

        if (oldRoot is not null && !child.HasAttachedParent)
        {
            ElementLifecycle.DetachSubtree(oldRoot, child);
            oldRoot.IncrementTreeVersion();
        }
        else
        {
            oldRoot?.IncrementTreeVersion();
        }

        InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Removed);
        Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
        return true;
    }

    public IEnumerator<UIElement> GetEnumerator()
    {
        return children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private UIElement? GetParent(UIElement child)
    {
        return role == ElementChildRole.Logical
            ? child.LogicalParent
            : child.VisualParent;
    }

    private bool IsAncestor(UIElement candidate)
    {
        Stack<UIElement> pending = new();
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);

        if (owner.LogicalParent is not null)
        {
            pending.Push(owner.LogicalParent);
        }

        if (owner.VisualParent is not null)
        {
            pending.Push(owner.VisualParent);
        }

        while (pending.Count > 0)
        {
            UIElement current = pending.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (ReferenceEquals(current, candidate))
            {
                return true;
            }

            if (current.LogicalParent is not null)
            {
                pending.Push(current.LogicalParent);
            }

            if (current.VisualParent is not null)
            {
                pending.Push(current.VisualParent);
            }
        }

        return false;
    }

    private void SetParent(UIElement child, UIElement? parent)
    {
        if (role == ElementChildRole.Logical)
        {
            child.SetLogicalParent(parent);
        }
        else
        {
            child.SetVisualParent(parent);
        }
    }

    private void InvalidateForVisualChildMutation(UIElement child, ElementTreeChangeKind kind)
    {
        if (role != ElementChildRole.Visual)
        {
            return;
        }

        string reason = kind == ElementTreeChangeKind.Added
            ? "Visual child added"
            : "Visual child removed";
        InvalidationFlags flags =
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Inherited;

        owner.IncrementLayoutVersion();
        owner.IncrementRenderVersion();
        owner.Invalidate(flags, reason);

        if (kind == ElementTreeChangeKind.Added && child.Root is not null)
        {
            child.Invalidate(flags | InvalidationFlags.Style | InvalidationFlags.Subtree, reason);
        }
    }

    private bool ContainsReference(UIElement child)
    {
        return IndexOfReference(child) >= 0;
    }

    private int IndexOfReference(UIElement child)
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (ReferenceEquals(children[i], child))
            {
                return i;
            }
        }

        return -1;
    }
}

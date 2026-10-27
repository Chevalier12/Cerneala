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

    // Raised after Move changed the order. Changed stays for additions and removals only, so an owner of an order
    // invariant, such as the overlay layer that must stay the root's last child (#932), listens here.
    internal event EventHandler? Moved;

    public int Count => children.Count;

    public UIElement this[int index] => children[index];

    // Changes with every insert, move and removal, as soon as the list changes, so a cache of the
    // list order (Panel z-order) can never outlive a mutation, even one whose callbacks throw.
    internal int Version { get; private set; }

    public void Add(UIElement child)
    {
        Insert(children.Count, child);
    }

    public void Insert(int index, UIElement child)
    {
        InsertCore(index, child, ownerManaged: false);
    }

    internal void InsertOwned(int index, UIElement child)
    {
        InsertCore(index, child, ownerManaged: true);
    }

    private void InsertCore(int index, UIElement child, bool ownerManaged)
    {
        ValidateInsertion(index, child, ownerManaged);
        owner.Root?.Motion.Presence.TryCancelExitForAdd(owner, child);
        children.Insert(index, child);
        Version++;
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

    internal void ValidateInsertion(int index, UIElement child, bool ownerManaged = false)
    {
        ArgumentNullException.ThrowIfNull(child);
        owner.VerifyOwnerAccess();
        child.VerifyOwnerAccess();
        if ((uint)index > (uint)children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        child.ValidateParentChange(owner, role, ownerManaged);
        owner.ValidateChildInsertion(child, role, ownerManaged);

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

        child.ReleaseFromParentForNewOwner();
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

        if (owner.Root is not null)
        {
            ElementLifecycle.ValidateSubtreeAttachment(owner.Root, child);
        }

    }

    public void Move(int oldIndex, int newIndex)
    {
        owner.VerifyOwnerAccess();
        if ((uint)oldIndex >= (uint)children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        }

        if ((uint)newIndex >= (uint)children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        }

        if (oldIndex == newIndex)
        {
            return;
        }

        UIElement child = children[oldIndex];
        child.ValidateParentChange(owner, role, ownerManaged: false);
        children.RemoveAt(oldIndex);
        children.Insert(newIndex, child);
        Version++;
        owner.Root?.IncrementTreeVersion();
        InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Added);
        Moved?.Invoke(this, EventArgs.Empty);
    }

    public bool Remove(UIElement child)
    {
        return RemoveCore(child, ownerManaged: false);
    }

    internal bool RemoveOwned(UIElement child)
    {
        return RemoveCore(child, ownerManaged: true);
    }

    private bool RemoveCore(UIElement child, bool ownerManaged)
    {
        ArgumentNullException.ThrowIfNull(child);
        owner.VerifyOwnerAccess();
        child.VerifyOwnerAccess();

        int index = IndexOfReference(child);
        if (index < 0)
        {
            return false;
        }

        child.ValidateParentChange(null, role, ownerManaged);

        if (role == ElementChildRole.Visual && owner.Root is UIRoot exitRoot && exitRoot.Motion.Presence.TryBeginExit(owner, child))
        {
            children.RemoveAt(index);
            Version++;
            SetParent(child, null);

            // An exit that is already complete detaches the child now, after the removal, as the detach below does.
            try
            {
                exitRoot.Motion.Presence.CompleteFinishedExit(child);
            }
            finally
            {
                exitRoot.IncrementTreeVersion();
                InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Removed);
                Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
            }

            return true;
        }

        children.RemoveAt(index);
        Version++;

        UIRoot? oldRoot = child.Root;
        SetParent(child, null);

        // A failing detach callback propagates only after the removal is fully published.
        try
        {
            if (oldRoot is not null && !child.HasAttachedParent)
            {
                ElementLifecycle.DetachSubtree(oldRoot, child);
            }
        }
        finally
        {
            oldRoot?.IncrementTreeVersion();
            InvalidateForVisualChildMutation(child, ElementTreeChangeKind.Removed);
            Changed?.Invoke(this, new ElementTreeChange(owner, child, role, ElementTreeChangeKind.Removed));
        }

        return true;
    }

    internal int IndexOf(UIElement child)
    {
        return IndexOfReference(child);
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
            child.Invalidate(flags | InvalidationFlags.Aspect | InvalidationFlags.Subtree, reason);
        }
    }

    private bool ContainsReference(UIElement child)
    {
        return IndexOfReference(child) >= 0;
    }

    private int IndexOfReference(UIElement child)
    {
        // Membership requires the parent for this collection's role. New or
        // differently parented nodes cannot occur in the list.
        if (!ReferenceEquals(GetParent(child), owner)) { return -1; }
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

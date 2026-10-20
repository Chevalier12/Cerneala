using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public sealed class InheritedPropertyPropagator
{
    public int PropagateFrom(UIElement root)
    {
        return PropagateFrom(root, null);
    }

    // A write raises the child's PropertyChanged, and the handler can change the tree, for example by removing its own
    // element or a later sibling, so the walk does not enumerate the live child lists. Like a recursive pre-order walk,
    // it reads an element's children after that element's writes. An element that a handler removed from the parent it
    // was reached through, that a handler cut off from the walk's root by removing an ancestor, or that a handler
    // detached from the walk's UIRoot gets no further writes, and neither does its subtree; a later add queues the
    // inherited work of its new place.
    internal int PropagateFrom(UIElement root, Action<UIElement>? beginDescendantPropagation)
    {
        ArgumentNullException.ThrowIfNull(root);
        WalkScope scope = new(root);
        Stack<(UIElement Parent, UIElement Child)> pending = new();
        PushChildren(pending, root);
        int changed = 0;
        while (pending.TryPop(out (UIElement Parent, UIElement Child) entry))
        {
            if (!scope.Reaches(entry.Parent, entry.Child))
            {
                continue;
            }

            changed += ApplyInheritedValues(entry.Parent, entry.Child, scope);
            if (!scope.Reaches(entry.Parent, entry.Child))
            {
                continue;
            }

            // Consume work covered by this traversal before descending. A later
            // callback can still requeue this element with genuinely new work.
            beginDescendantPropagation?.Invoke(entry.Child);
            PushChildren(pending, entry.Child);
        }

        return changed;
    }

    private static void PushChildren(Stack<(UIElement Parent, UIElement Child)> pending, UIElement parent)
    {
        UIElementCollection children = parent.VisualChildren;
        for (int index = children.Count - 1; index >= 0; index--)
        {
            pending.Push((parent, children[index]));
        }
    }

    private static int ApplyInheritedValues(UIElement parent, UIElement child, in WalkScope scope)
    {
        int changed = 0;
        foreach (UiProperty property in UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.Inherits))
        {
            object? oldEffective = child.GetValue(property);
            UiPropertyValueSource parentSource = parent.GetValueSource(property);
            if (parentSource == UiPropertyValueSource.Default)
            {
                child.ClearValueUntyped(property, UiPropertyValueSource.Inherited);
            }
            else
            {
                child.SetValueUntyped(property, parent.GetValue(property), UiPropertyValueSource.Inherited);
            }

            if (!property.AreEqualUntyped(oldEffective, child.GetValue(property)))
            {
                changed++;
            }

            if (!scope.Reaches(parent, child))
            {
                break;
            }
        }

        return changed;
    }

    // Whether a child is still in the walk: its parent is the one it was reached through, it is in the walk root's
    // UIRoot, and its ancestors still lead to the walk root. A removed ancestor can stay in the UIRoot, for example
    // while its Presence exit runs, so the root check alone does not show it. Every visual parent change in an attached
    // tree raises the UIRoot's TreeVersion, so while that version is unchanged no ancestor has moved and the ancestor
    // chain is not walked. A walk whose root is not attached walks it on every check.
    private readonly struct WalkScope
    {
        private readonly UIElement root;
        private readonly UIRoot? tree;
        private readonly int treeVersion;

        public WalkScope(UIElement root)
        {
            this.root = root;
            tree = root.Root;
            treeVersion = tree?.TreeVersion ?? 0;
        }

        public bool Reaches(UIElement parent, UIElement child)
        {
            if (!ReferenceEquals(child.VisualParent, parent) || !ReferenceEquals(child.Root, tree))
            {
                return false;
            }

            if (tree is not null && tree.TreeVersion == treeVersion)
            {
                return true;
            }

            for (UIElement? ancestor = parent; !ReferenceEquals(ancestor, root); ancestor = ancestor.VisualParent)
            {
                if (ancestor is null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

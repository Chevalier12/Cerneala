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
    // was reached through, or detached from the walk's root, gets no further writes, and neither does its subtree; a
    // later add queues the inherited work of its new place.
    internal int PropagateFrom(UIElement root, Action<UIElement>? beginDescendantPropagation)
    {
        ArgumentNullException.ThrowIfNull(root);
        UIRoot? tree = root.Root;
        Stack<(UIElement Parent, UIElement Child)> pending = new();
        PushChildren(pending, root);
        int changed = 0;
        while (pending.TryPop(out (UIElement Parent, UIElement Child) entry))
        {
            if (!IsReachedThrough(entry.Parent, entry.Child, tree))
            {
                continue;
            }

            changed += ApplyInheritedValues(entry.Parent, entry.Child, tree);
            if (!IsReachedThrough(entry.Parent, entry.Child, tree))
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

    private static bool IsReachedThrough(UIElement parent, UIElement child, UIRoot? tree)
    {
        return ReferenceEquals(child.VisualParent, parent) && ReferenceEquals(child.Root, tree);
    }

    private static int ApplyInheritedValues(UIElement parent, UIElement child, UIRoot? tree)
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

            if (!IsReachedThrough(parent, child, tree))
            {
                break;
            }
        }

        return changed;
    }
}

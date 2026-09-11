using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public sealed class InheritedPropertyPropagator
{
    public int PropagateFrom(UIElement root)
    {
        return PropagateFrom(root, null);
    }

    internal int PropagateFrom(UIElement root, Action<UIElement>? beginDescendantPropagation)
    {
        ArgumentNullException.ThrowIfNull(root);
        int changed = 0;
        foreach (UIElement child in root.VisualChildren)
        {
            changed += PropagateToSubtree(root, child, beginDescendantPropagation);
        }

        return changed;
    }

    private static int PropagateToSubtree(
        UIElement parent,
        UIElement child,
        Action<UIElement>? beginDescendantPropagation)
    {
        int changed = ApplyInheritedValues(parent, child);
        // Consume work covered by this traversal before descending. A later
        // callback can still requeue this element with genuinely new work.
        beginDescendantPropagation?.Invoke(child);
        foreach (UIElement grandchild in child.VisualChildren)
        {
            changed += PropagateToSubtree(child, grandchild, beginDescendantPropagation);
        }

        return changed;
    }

    private static int ApplyInheritedValues(UIElement parent, UIElement child)
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
        }

        return changed;
    }
}

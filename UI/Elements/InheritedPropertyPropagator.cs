using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public sealed class InheritedPropertyPropagator
{
    public int PropagateFrom(UIElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        int changed = 0;
        foreach (UIElement child in root.VisualChildren)
        {
            changed += PropagateToSubtree(root, child);
        }

        return changed;
    }

    private static int PropagateToSubtree(UIElement parent, UIElement child)
    {
        int changed = ApplyInheritedValues(parent, child);
        foreach (UIElement grandchild in child.VisualChildren)
        {
            changed += PropagateToSubtree(child, grandchild);
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

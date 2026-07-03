using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class PseudoClassRegistry
{
    private readonly Dictionary<UiProperty, PseudoClass> properties = new(ReferenceEqualityComparer.Instance);

    public static PseudoClassRegistry Default { get; } = CreateDefault();

    public void Register(UiProperty property, PseudoClass pseudoClass)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (pseudoClass == PseudoClass.None)
        {
            throw new ArgumentOutOfRangeException(nameof(pseudoClass), "Pseudo class registration requires a non-empty pseudo class.");
        }

        properties[property] = pseudoClass;
    }

    public bool AffectsPseudoClass(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return properties.ContainsKey(property);
    }

    public PseudoClass GetPseudoClasses(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return properties.TryGetValue(property, out PseudoClass pseudoClass)
            ? pseudoClass
            : PseudoClass.None;
    }

    private static PseudoClassRegistry CreateDefault()
    {
        PseudoClassRegistry registry = new();
        registry.Register(UIElement.IsPointerOverProperty, PseudoClass.Hover);
        registry.Register(UIElement.IsKeyboardFocusedProperty, PseudoClass.Focus);
        registry.Register(UIElement.IsKeyboardFocusWithinProperty, PseudoClass.FocusWithin);
        registry.Register(UIElement.IsEnabledProperty, PseudoClass.Disabled);
        registry.Register(ButtonBase.IsPressedProperty, PseudoClass.Pressed);
        registry.Register(ListBoxItem.IsSelectedProperty, PseudoClass.Selected);
        registry.Register(TabItem.IsSelectedProperty, PseudoClass.Selected);
        return registry;
    }
}

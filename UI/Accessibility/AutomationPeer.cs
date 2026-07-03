using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Accessibility;

public class AutomationPeer
{
    public AutomationPeer(UIElement owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public UIElement Owner { get; }

    public virtual SemanticsRole Role => Owner is UIRoot ? SemanticsRole.Root : SemanticsRole.Group;

    public virtual string? Name => AccessibleName.GetName(Owner);

    public virtual IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new()
        {
            [SemanticsProperty.IsEnabled] = Owner.IsEnabled,
            [SemanticsProperty.IsFocused] = Owner.IsKeyboardFocused
        };
        return properties;
    }

    public static AutomationPeer Create(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element switch
        {
            Button button => new ButtonAutomationPeer(button),
            TextBoxBase textBox => new TextBoxAutomationPeer(textBox),
            ItemsControl itemsControl => new ItemsControlAutomationPeer(itemsControl),
            TextBlock => new AutomationPeer(element) { OverrideRole = SemanticsRole.Text },
            _ => new AutomationPeer(element)
        };
    }

    protected SemanticsRole? OverrideRole { get; init; }

    protected SemanticsRole EffectiveRole => OverrideRole ?? Role;

    public SemanticsNode CreateNode(IReadOnlyList<SemanticsNode> children)
    {
        return new SemanticsNode(Owner.ElementId, EffectiveRole, Name, GetProperties(), children);
    }
}

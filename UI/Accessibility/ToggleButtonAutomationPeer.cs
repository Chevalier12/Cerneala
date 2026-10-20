using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

namespace Cerneala.UI.Accessibility;

public sealed class ToggleButtonAutomationPeer : ButtonAutomationPeer
{
    private readonly ToggleButton toggleButton;

    public ToggleButtonAutomationPeer(ToggleButton toggleButton)
        : base(toggleButton)
    {
        this.toggleButton = toggleButton;
    }

    // A plain ToggleButton keeps the Button role, as WPF's UI Automation control type does.
    public override SemanticsRole Role => toggleButton switch
    {
        CheckBox => SemanticsRole.CheckBox,
        RadioButton => SemanticsRole.RadioButton,
        _ => SemanticsRole.Button
    };

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.IsChecked] = toggleButton.IsChecked
        };
        return properties;
    }
}

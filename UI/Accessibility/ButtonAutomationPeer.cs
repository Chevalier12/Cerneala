using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.UI.Accessibility;

public sealed class ButtonAutomationPeer : AutomationPeer
{
    private readonly Button button;

    public ButtonAutomationPeer(Button button)
        : base(button)
    {
        this.button = button;
    }

    public override SemanticsRole Role => SemanticsRole.Button;

    public override string? Name => AccessibleName.GetName(button) ?? AccessibleName.GetContentText(button.Content);

    public bool Invoke()
    {
        if (!button.IsEnabled)
        {
            return false;
        }

        ((IInputActivatable)button).Activate();
        return true;
    }
}

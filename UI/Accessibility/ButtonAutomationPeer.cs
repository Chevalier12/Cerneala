using Cerneala.UI.Controls;

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
}

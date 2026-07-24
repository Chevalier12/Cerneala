using Cerneala.UI.Controls;

namespace Cerneala.UI.Accessibility;

public sealed class PasswordBoxAutomationPeer : AutomationPeer
{
    public PasswordBoxAutomationPeer(PasswordBox passwordBox)
        : base(passwordBox)
    {
    }

    public override SemanticsRole Role => SemanticsRole.EditableText;

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.Value] = null
        };
        return properties;
    }
}

using Cerneala.UI.Controls;

namespace Cerneala.UI.Accessibility;

public sealed class TextBoxAutomationPeer : AutomationPeer
{
    private readonly TextBoxBase textBox;

    public TextBoxAutomationPeer(TextBoxBase textBox)
        : base(textBox)
    {
        this.textBox = textBox;
    }

    public override SemanticsRole Role => SemanticsRole.EditableText;

    public override IReadOnlyDictionary<SemanticsProperty, object?> GetProperties()
    {
        Dictionary<SemanticsProperty, object?> properties = new(base.GetProperties())
        {
            [SemanticsProperty.Value] = textBox is PasswordBox ? null : textBox.Text
        };
        return properties;
    }
}

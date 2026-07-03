using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public abstract class ControlTemplate
{
    private protected ControlTemplate(Type ownerType)
    {
        if (!typeof(Control).IsAssignableFrom(ownerType))
        {
            throw new ArgumentException("Template owner type must derive from Control.", nameof(ownerType));
        }

        OwnerType = ownerType;
    }

    public Type OwnerType { get; }

    public TemplateInstance CreateInstance(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!OwnerType.IsInstanceOfType(owner))
        {
            throw new InvalidOperationException(
                $"Template for '{OwnerType.FullName}' cannot be applied to '{owner.GetType().FullName}'.");
        }

        return CreateInstanceCore(owner);
    }

    private protected abstract TemplateInstance CreateInstanceCore(Control owner);
}

using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplateSlotMap
{
    private readonly Dictionary<AspectSlot, UIElement> slots = [];

    internal IEnumerable<KeyValuePair<AspectSlot, UIElement>> Entries => slots;

    public UIElement this[AspectSlot slot] => slots[slot];

    public void Register(AspectSlot slot, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(element);
        if (!slot.TargetType.IsInstanceOfType(element))
        {
            throw new ArgumentException(
                $"Aspect slot '{slot}' expects an element assignable to '{slot.TargetType.FullName}', but received '{element.GetType().FullName}'.",
                nameof(element));
        }

        slots[slot] = element;
    }
}

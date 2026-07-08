using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplateSlotMap
{
    private readonly Dictionary<AspectSlot, UIElement> slots = [];

    public UIElement this[AspectSlot slot] => slots[slot];

    public void Register(AspectSlot slot, UIElement element)
    {
        slots[slot ?? throw new ArgumentNullException(nameof(slot))] = element ?? throw new ArgumentNullException(nameof(element));
    }
}

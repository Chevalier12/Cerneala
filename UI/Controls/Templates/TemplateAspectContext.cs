using System.Runtime.CompilerServices;
using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

internal static class TemplateAspectContext
{
    private static readonly ConditionalWeakTable<UIElement, Registration> Registrations = new();

    public static void Attach(Control owner, AspectSlot slot, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(element);
        if (Registrations.TryGetValue(element, out _))
        {
            throw new InvalidOperationException("A template element cannot be registered in more than one Aspect slot.");
        }

        Registrations.Add(
            element,
            new Registration(owner, new AspectSlotPath(slot, $"{owner.GetType().Name}/{slot.Name}")));
    }

    public static bool TryGet(UIElement element, out Registration registration)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Registrations.TryGetValue(element, out registration!);
    }

    public static void Detach(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Registrations.Remove(element);
    }

    internal sealed record Registration(Control Owner, AspectSlotPath SlotPath);
}

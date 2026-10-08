using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

// Element -> component template owner, for `$owner` in an Aspect program that
// is compiled once and applied at runtime. Registered when a template
// instance is attached to its owner, before its root joins the visual tree,
// and removed when the instance detaches. Elements of a nested template keep
// the owner registered by their own instance.
internal static class TemplateOwnership
{
    private static readonly ConditionalWeakTable<UIElement, Control> Owners = new();

    public static void Register(Control owner, UIElement root)
    {
        foreach (UIElement element in Subtree(root))
        {
            _ = Owners.TryAdd(element, owner);
        }
    }

    public static void Unregister(Control owner, UIElement root)
    {
        foreach (UIElement element in Subtree(root))
        {
            if (Owners.TryGetValue(element, out Control? current) && ReferenceEquals(current, owner))
            {
                Owners.Remove(element);
            }
        }
    }

    public static bool TryGetOwner(UIElement element, out Control? owner) =>
        Owners.TryGetValue(element, out owner);

    private static IEnumerable<UIElement> Subtree(UIElement root)
    {
        Stack<UIElement> pending = new();
        pending.Push(root);
        while (pending.Count > 0)
        {
            UIElement element = pending.Pop();
            yield return element;
            foreach (UIElement child in element.VisualChildren)
            {
                pending.Push(child);
            }
        }
    }
}

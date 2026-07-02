using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

internal static class ElementQueueOrder
{
    public static void RemoveElementsOutsideRoot(
        UIElement root,
        HashSet<UIElement> elements,
        List<UIElement> order)
    {
        _ = order.RemoveAll(element =>
        {
            if (ReferenceEquals(element.Root, root))
            {
                return false;
            }

            elements.Remove(element);
            return true;
        });

        elements.RemoveWhere(element => !ReferenceEquals(element.Root, root));
    }

    public static IReadOnlyList<UIElement> Sort(UIElement root, IEnumerable<UIElement> elements)
    {
        Dictionary<UIElement, int> order = new(ReferenceEqualityComparer.Instance);
        int index = 0;
        foreach (UIElement element in ElementTreeWalker.PreOrder(root, ElementChildRole.Visual))
        {
            order[element] = index++;
        }

        return elements
            .Select((Element, Index) => new ElementOrder(Element, Index))
            .OrderBy(item => order.TryGetValue(item.Element, out int treeIndex) ? treeIndex : int.MaxValue)
            .ThenBy(item => item.Index)
            .Select(item => item.Element)
            .ToArray();
    }

    private readonly record struct ElementOrder(UIElement Element, int Index);
}

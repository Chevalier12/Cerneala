namespace Cerneala.UI.Elements;

public static class ElementTreeWalker
{
    public static IEnumerable<UIElement> PreOrder(UIElement root, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        return PreOrderIterator(root, role);
    }

    public static IEnumerable<UIElement> PostOrder(UIElement root, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        return PostOrderIterator(root, role);
    }

    public static IEnumerable<UIElement> Ancestors(UIElement element, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(element);

        return AncestorsIterator(element, role);
    }

    public static IEnumerable<UIElement> Descendants(UIElement root, ElementChildRole role = ElementChildRole.Visual)
    {
        ArgumentNullException.ThrowIfNull(root);

        return DescendantsIterator(root, role);
    }

    private static IEnumerable<UIElement> PreOrderIterator(UIElement root, ElementChildRole role)
    {
        yield return root;
        foreach (UIElement child in Children(root, role))
        {
            foreach (UIElement descendant in PreOrderIterator(child, role))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<UIElement> PostOrderIterator(UIElement root, ElementChildRole role)
    {
        foreach (UIElement child in Children(root, role))
        {
            foreach (UIElement descendant in PostOrderIterator(child, role))
            {
                yield return descendant;
            }
        }

        yield return root;
    }

    private static IEnumerable<UIElement> AncestorsIterator(UIElement element, ElementChildRole role)
    {
        UIElement? current = role == ElementChildRole.Logical
            ? element.LogicalParent
            : element.VisualParent;

        while (current is not null)
        {
            yield return current;
            current = role == ElementChildRole.Logical
                ? current.LogicalParent
                : current.VisualParent;
        }
    }

    private static IEnumerable<UIElement> DescendantsIterator(UIElement root, ElementChildRole role)
    {
        foreach (UIElement child in Children(root, role))
        {
            yield return child;
            foreach (UIElement descendant in DescendantsIterator(child, role))
            {
                yield return descendant;
            }
        }
    }

    private static IReadOnlyList<UIElement> Children(UIElement element, ElementChildRole role)
    {
        return role == ElementChildRole.Logical
            ? element.LogicalChildren
            : element.VisualChildren;
    }
}

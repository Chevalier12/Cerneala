using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Accessibility;

public sealed class SemanticsProvider
{
    public SemanticsTree Build(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new SemanticsTree(BuildNode(root));
    }

    private static SemanticsNode BuildNode(UIElement element)
    {
        List<SemanticsNode> children = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (!IsVisibleForSemantics(child))
            {
                continue;
            }

            children.Add(BuildNode(child));
        }

        return AutomationPeer.Create(element).CreateNode(children);
    }

    private static bool IsVisibleForSemantics(UIElement element)
    {
        return element.IsVisible && element.Visibility == Visibility.Visible;
    }
}

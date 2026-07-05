using Cerneala.UI.Elements;

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
            if (!UIElementVisibility.ParticipatesInRendering(child))
            {
                continue;
            }

            children.Add(BuildNode(child));
        }

        return AutomationPeer.Create(element).CreateNode(children);
    }
}

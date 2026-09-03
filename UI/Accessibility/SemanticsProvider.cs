using Cerneala.UI.Elements;

namespace Cerneala.UI.Accessibility;

public sealed class SemanticsProvider
{
    public SemanticsTree Build(UIRoot root)
    {
        return Build(root, SemanticsProjection.Accessibility);
    }

    internal SemanticsTree Build(UIRoot root, SemanticsProjection projection)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new SemanticsTree(BuildNode(root, projection));
    }

    private static SemanticsNode BuildNode(UIElement element, SemanticsProjection projection)
    {
        List<SemanticsNode> children = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (projection == SemanticsProjection.Accessibility &&
                !UIElementVisibility.ParticipatesInRendering(child))
            {
                continue;
            }

            children.Add(BuildNode(child, projection));
        }

        return AutomationPeer.Create(element).CreateNode(children);
    }
}

internal enum SemanticsProjection
{
    Accessibility,
    Servo
}

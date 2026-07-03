namespace Cerneala.UI.Accessibility;

public sealed class SemanticsTree
{
    public SemanticsTree(SemanticsNode root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SemanticsNode Root { get; }
}

using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

internal sealed class ElementQueueOrderIndex
{
    private readonly UIRoot root;
    private Dictionary<UIElement, int> preorderOrdinals = new(ReferenceEqualityComparer.Instance);
    private int indexedTreeVersion = -1;

    public ElementQueueOrderIndex(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    internal int BuildCount { get; private set; }

    internal int LastVisitedNodeCount { get; private set; }

    internal long TotalVisitedNodeCount { get; private set; }

    internal void EnsureCurrent()
    {
        if (indexedTreeVersion == root.TreeVersion)
        {
            return;
        }

        Dictionary<UIElement, int> rebuilt = new(ReferenceEqualityComparer.Instance);
        int ordinal = 0;
        foreach (UIElement element in ElementTreeWalker.PreOrder(root, ElementChildRole.Visual))
        {
            rebuilt[element] = ordinal++;
        }

        preorderOrdinals = rebuilt;
        indexedTreeVersion = root.TreeVersion;
        BuildCount++;
        LastVisitedNodeCount = ordinal;
        TotalVisitedNodeCount += ordinal;
    }

    internal bool TryGetOrdinal(UIElement element, out int ordinal)
    {
        return preorderOrdinals.TryGetValue(element, out ordinal);
    }
}

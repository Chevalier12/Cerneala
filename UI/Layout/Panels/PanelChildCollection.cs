using System.Collections;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

// The children of a panel, like WPF Panel.Children: one call adds or removes a child in the
// visual tree, which the panel lays out and draws, and in the logical tree together.
public sealed class PanelChildCollection : IReadOnlyList<UIElement>
{
    private readonly Panel owner;

    internal PanelChildCollection(Panel owner)
    {
        this.owner = owner;
    }

    public int Count => owner.VisualChildren.Count;

    public UIElement this[int index] => owner.VisualChildren[index];

    public void Add(UIElement child)
    {
        Insert(owner.VisualChildren.Count, child);
    }

    public void Insert(int index, UIElement child)
    {
        // Validate the visual insertion first, so a rejected child never becomes a logical-only child.
        owner.VisualChildren.ValidateInsertion(index, child);
        owner.LogicalChildren.InsertOwned(GetLogicalIndex(index), child);
        try
        {
            owner.VisualChildren.Insert(index, child);
        }
        catch
        {
            owner.LogicalChildren.Remove(child);
            throw;
        }
    }

    public bool Remove(UIElement child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child.VisualParent, owner))
        {
            return false;
        }

        // Logical first: the child stays attached through its visual parent, so the visual removal can start a
        // presence exit. In the other order the logical removal detaches the child and cancels that exit.
        if (ReferenceEquals(child.LogicalParent, owner))
        {
            owner.LogicalChildren.Remove(child);
        }

        owner.VisualChildren.Remove(child);
        return true;
    }

    public IEnumerator<UIElement> GetEnumerator()
    {
        return owner.VisualChildren.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // The logical position follows the visual position: after the logical children that come before it visually.
    private int GetLogicalIndex(int visualIndex)
    {
        int logicalIndex = 0;
        for (int index = 0; index < visualIndex; index++)
        {
            if (ReferenceEquals(owner.VisualChildren[index].LogicalParent, owner))
            {
                logicalIndex++;
            }
        }

        return logicalIndex;
    }
}

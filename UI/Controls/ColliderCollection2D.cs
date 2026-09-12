using System.Collections.ObjectModel;

namespace Cerneala.UI.Controls;

// This collection owns both the logical attachment and collision mutation.
internal sealed class ColliderCollection2D(SceneNode2D owner) : Collection<Collider2D>
{
    protected override void InsertItem(int index, Collider2D item)
    {
        ArgumentNullException.ThrowIfNull(item);
        owner.LogicalChildren.InsertOwned(index, item);
        base.InsertItem(index, item);
        item.AttachSurface(owner.Surface);
        NotifyStructureChanged();
    }

    protected override void SetItem(int index, Collider2D item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Collider2D previous = this[index];
        if (ReferenceEquals(previous, item)) { return; }
        owner.LogicalChildren.ValidateInsertion(index, item, ownerManaged: true);
        previous.AttachSurface(null);
        owner.LogicalChildren.RemoveOwned(previous);
        owner.LogicalChildren.InsertOwned(index, item);
        base.SetItem(index, item);
        item.AttachSurface(owner.Surface);
        NotifyStructureChanged();
    }

    protected override void RemoveItem(int index)
    {
        Collider2D previous = this[index];
        previous.AttachSurface(null);
        owner.LogicalChildren.RemoveOwned(previous);
        base.RemoveItem(index);
        NotifyStructureChanged();
    }

    protected override void ClearItems()
    {
        if (Count == 0) { return; }
        foreach (Collider2D collider in this)
        {
            collider.AttachSurface(null);
            owner.LogicalChildren.RemoveOwned(collider);
        }
        base.ClearItems();
        NotifyStructureChanged();
    }

    private void NotifyStructureChanged()
    {
        if (owner is TileInstance2D tile && tile.OwnerLayer?.OwnerMap is TileMap2D map)
        {
            map.SynchronizeCollisionAdaptersAndNotify();
        }
        else
        {
            SceneGeometry2D.FindRootScene(owner)?.NotifyCollisionMutation(owner, SceneCollisionMutationKind.Structure);
        }
        owner.Surface?.InvalidateFrame();
    }
}

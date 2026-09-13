namespace Cerneala.UI.Controls;

// Owns the single logical collider attachment and its collision mutation.
internal sealed class ColliderSlot2D(Sprite2D owner)
{
    private Collider2D? value;

    internal Collider2D? Value
    {
        get => value;
        set => Replace(value);
    }

    internal void AttachSurface(RenderSurface2D? surface) => value?.AttachSurface(surface);

    private void Replace(Collider2D? replacement)
    {
        if (ReferenceEquals(value, replacement)) { return; }

        int index = owner.LogicalChildren.Count;
        if (value is not null)
        {
            index = 0;
            while (index < owner.LogicalChildren.Count && !ReferenceEquals(owner.LogicalChildren[index], value))
            {
                index++;
            }
            if (index == owner.LogicalChildren.Count)
            {
                throw new InvalidOperationException("The collider slot is inconsistent with the owner's logical children.");
            }
        }
        if (replacement is not null)
        {
            owner.LogicalChildren.ValidateInsertion(index, replacement, ownerManaged: true);
        }

        Collider2D? previous = value;
        if (previous is not null)
        {
            previous.AttachSurface(null);
            if (!owner.LogicalChildren.RemoveOwned(previous))
            {
                throw new InvalidOperationException("The collider could not be detached from its owner.");
            }
        }
        if (replacement is not null)
        {
            owner.LogicalChildren.InsertOwned(index, replacement);
            replacement.AttachSurface(owner.Surface);
        }
        value = replacement;
        NotifyStructureChanged();
    }

    private void NotifyStructureChanged()
    {
        SceneGeometry2D.FindRootScene(owner)?.NotifyCollisionMutation(owner, SceneCollisionMutationKind.Structure);
        owner.Surface?.InvalidateFrame();
    }
}

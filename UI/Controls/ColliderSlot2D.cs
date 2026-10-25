namespace Cerneala.UI.Controls;

// What a slot holds: the owner's body, or the owner's cone-shaped sensor.
internal enum ColliderSlotRole2D
{
    Body,
    Vision
}

// Owns one logical collider attachment of a Sprite2D or Scene2D and its collision mutation.
internal sealed class ColliderSlot2D(SceneNode2D owner, ColliderSlotRole2D role)
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

        // Reject before anything changes, so the collider already in the slot stays attached.
        if (role == ColliderSlotRole2D.Body && replacement is ConeCollider2D)
        {
            throw new InvalidOperationException(
                "ConeCollider2D can only be a Vision: assign it to Sprite2D.Vision or Scene2D.Vision, not to Sprite2D.Collider.");
        }

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
            previous.VisionOwner = null;
        }
        if (replacement is not null)
        {
            // Set before the insertion, so the collision world never sees the cone as a body.
            if (role == ColliderSlotRole2D.Vision) { replacement.VisionOwner = owner; }
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

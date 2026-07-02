namespace Cerneala.UI.Invalidation;

public sealed class DirtyState
{
    public InvalidationFlags Flags { get; private set; }

    public long Version { get; private set; }

    public bool IsDirty => Flags != InvalidationFlags.None;

    public bool Has(InvalidationFlags flags)
    {
        return flags != InvalidationFlags.None && (Flags & flags) == flags;
    }

    public bool Mark(InvalidationFlags flags)
    {
        flags &= ~InvalidationFlags.None;
        if (flags == InvalidationFlags.None)
        {
            return false;
        }

        InvalidationFlags next = Flags | flags;
        if (next == Flags)
        {
            return false;
        }

        Flags = next;
        Version++;
        return true;
    }

    public bool Clear(InvalidationFlags flags)
    {
        flags &= ~InvalidationFlags.None;
        if (flags == InvalidationFlags.None)
        {
            return false;
        }

        InvalidationFlags next = Flags & ~flags;
        if (next == Flags)
        {
            return false;
        }

        Flags = next;
        return true;
    }

    public void ClearAll()
    {
        Flags = InvalidationFlags.None;
    }
}

namespace Cerneala.UI.Prism.Definitions;

internal readonly record struct PrismParameterKey<T>
{
    public PrismParameterKey(int entryStableId, int slot)
    {
        if (entryStableId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(entryStableId));
        }
        if (slot < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        EntryStableId = entryStableId;
        Slot = slot;
    }

    public int EntryStableId { get; }

    public int Slot { get; }
}

using System.Collections.ObjectModel;

namespace Cerneala.Drawing.Prism;

public sealed class PrismPipeline : Collection<PrismOperation>
{
    private long topologyVersion;

    internal event EventHandler? Changed;

    public PrismPipeline()
    {
    }

    public PrismPipeline(IEnumerable<PrismOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        foreach (PrismOperation operation in operations)
        {
            Add(operation);
        }
    }

    internal long TopologyVersion => topologyVersion;

    internal long ContentSignature
    {
        get
        {
            long signature = topologyVersion;
            foreach (PrismOperation operation in Items)
            {
                signature = Mix(signature, operation.Version);
            }

            return signature;
        }
    }

    protected override void InsertItem(int index, PrismOperation item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item);
        item.Changed += OnOperationChanged;
        MarkTopologyChanged();
    }

    protected override void SetItem(int index, PrismOperation item)
    {
        ArgumentNullException.ThrowIfNull(item);
        PrismOperation previous = this[index];
        previous.Changed -= OnOperationChanged;
        base.SetItem(index, item);
        item.Changed += OnOperationChanged;
        MarkTopologyChanged();
    }

    protected override void RemoveItem(int index)
    {
        PrismOperation removed = this[index];
        removed.Changed -= OnOperationChanged;
        base.RemoveItem(index);
        MarkTopologyChanged();
    }

    protected override void ClearItems()
    {
        if (Count == 0)
        {
            return;
        }

        foreach (PrismOperation operation in Items)
        {
            operation.Changed -= OnOperationChanged;
        }

        base.ClearItems();
        MarkTopologyChanged();
    }

    private void MarkTopologyChanged()
    {
        unchecked
        {
            topologyVersion++;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnOperationChanged(object? sender, EventArgs args) =>
        Changed?.Invoke(this, EventArgs.Empty);

    private static long Mix(long current, long value)
    {
        unchecked
        {
            ulong hash = (ulong)current;
            hash ^= (ulong)value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2);
            return (long)(hash & long.MaxValue);
        }
    }
}

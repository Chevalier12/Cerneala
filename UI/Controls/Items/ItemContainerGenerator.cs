using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls.Items;

public sealed class ItemContainerGenerator
{
    private static readonly ConditionalWeakTable<UIElement, ItemContainerInfo> containerInfo = new();
    private readonly ItemsControl owner;
    private readonly Dictionary<int, UIElement> realized = [];

    public ItemContainerGenerator(ItemsControl owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        RecyclePool = new ItemContainerRecyclePool();
    }

    public ItemContainerRecyclePool RecyclePool { get; }

    public IReadOnlyDictionary<int, UIElement> RealizedContainers => realized;

    public IReadOnlyList<UIElement> Realize(RealizationWindow? window = null)
    {
        int count = owner.ViewItemCount;
        int start = window?.StartIndex ?? 0;
        int end = window?.EndIndexExclusive ?? count;
        start = Math.Clamp(start, 0, count);
        end = Math.Clamp(end, start, count);
        HashSet<int> desired = [.. Enumerable.Range(start, end - start)
            .Select(owner.GetSourceIndexForViewIndex)];

        MoveShiftedItemContainers(desired);
        foreach (int index in realized.Keys.Where(index => !desired.Contains(index)).ToArray())
        {
            Recycle(index);
        }

        List<UIElement> containers = [];
        for (int viewIndex = start; viewIndex < end; viewIndex++)
        {
            containers.Add(GetOrCreate(owner.GetSourceIndexForViewIndex(viewIndex)));
        }

        return containers;
    }

    public UIElement GetOrCreate(int index)
    {
        if (index < 0 || index >= owner.ItemCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        object? item = owner.GetItemAt(index);
        Type containerType = owner.GetContainerTypeForItem(item);
        if (realized.TryGetValue(index, out UIElement? existing))
        {
            if (IsCompatibleContainer(existing, item, containerType))
            {
                if (!ReferenceEquals(GetItem(existing), item) || GetItemIndex(existing) != index)
                {
                    owner.PrepareItemContainer(existing, index, item);
                }

                return existing;
            }

            Recycle(index);
        }

        UIElement container = RecyclePool.Pop(containerType) ?? owner.CreateItemContainer(index, item);
        owner.PrepareItemContainer(container, index, item);
        realized.Add(index, container);
        return container;
    }

    public void Recycle(int index)
    {
        if (!realized.Remove(index, out UIElement? container))
        {
            return;
        }

        object? item = GetItem(container);
        DetachContainer(container);
        owner.ClearItemContainer(container);
        if (!ReferenceEquals(container, item))
        {
            RecyclePool.Push(container);
        }
    }

    public void Clear()
    {
        foreach (int index in realized.Keys.ToArray())
        {
            Recycle(index);
        }
    }

    // The owner reports each item change whose position is known, so realized containers stay keyed by the
    // current index of their item. A container for a replaced item keeps its index and is prepared for the new
    // item by the next realization; a container that moved to another index is prepared for that index then.
    internal void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        for (int removedIndex = index + Math.Min(removedCount, addedCount); removedIndex < index + removedCount; removedIndex++)
        {
            Recycle(removedIndex);
        }

        int firstShiftedIndex = index + removedCount;
        int shift = addedCount - removedCount;
        if (shift != 0)
        {
            Rekey(realizedIndex => realizedIndex >= firstShiftedIndex ? realizedIndex + shift : realizedIndex);
        }
    }

    internal void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        Rekey(realizedIndex =>
        {
            if (realizedIndex >= oldIndex && realizedIndex < oldIndex + count)
            {
                return newIndex + realizedIndex - oldIndex;
            }

            realizedIndex -= realizedIndex >= oldIndex + count ? count : 0;
            return realizedIndex + (realizedIndex >= newIndex ? count : 0);
        });
    }

    private void Rekey(Func<int, int> getNewIndex)
    {
        if (realized.Count == 0)
        {
            return;
        }

        KeyValuePair<int, UIElement>[] entries = [.. realized];
        realized.Clear();
        foreach ((int index, UIElement container) in entries)
        {
            realized.Add(getNewIndex(index), container);
        }
    }

    public static int GetItemIndex(UIElement container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return containerInfo.TryGetValue(container, out ItemContainerInfo? info) ? info.Index : -1;
    }

    public static object? GetItem(UIElement container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return containerInfo.TryGetValue(container, out ItemContainerInfo? info) ? info.Item : null;
    }

    public static bool GetIsSelected(UIElement container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return containerInfo.TryGetValue(container, out ItemContainerInfo? info) && info.IsSelected;
    }

    public static void SetInfo(UIElement container, int index, object? item, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(container);
        ItemContainerInfo info = containerInfo.GetOrCreateValue(container);
        info.Index = index;
        info.Item = item;
        info.IsSelected = isSelected;
    }

    public static void ClearInfo(UIElement container)
    {
        ArgumentNullException.ThrowIfNull(container);
        ItemContainerInfo info = containerInfo.GetOrCreateValue(container);
        info.Index = -1;
        info.Item = null;
        info.IsSelected = false;
    }

    // A container that is its own item belongs to that item, not to an index. When the item moves to
    // another desired index, the container moves with it instead of being recycled from its old index.
    private void MoveShiftedItemContainers(HashSet<int> desired)
    {
        List<(int Index, UIElement Container)>? moves = null;
        foreach (int index in desired)
        {
            if (owner.GetItemAt(index) is not UIElement element ||
                !containerInfo.TryGetValue(element, out ItemContainerInfo? info) ||
                !ReferenceEquals(info.Item, element) ||
                info.Index == index ||
                !realized.TryGetValue(info.Index, out UIElement? current) ||
                !ReferenceEquals(current, element) ||
                (info.Index < owner.ItemCount && ReferenceEquals(owner.GetItemAt(info.Index), element)) ||
                ContainsMovedContainer(moves, element))
            {
                continue;
            }

            (moves ??= []).Add((index, element));
        }

        if (moves is null)
        {
            return;
        }

        foreach ((_, UIElement container) in moves)
        {
            realized.Remove(GetItemIndex(container));
        }

        foreach ((int index, UIElement container) in moves)
        {
            Recycle(index);
            realized.Add(index, container);
        }
    }

    private static bool ContainsMovedContainer(List<(int Index, UIElement Container)>? moves, UIElement container)
    {
        if (moves is null)
        {
            return false;
        }

        foreach ((_, UIElement moved) in moves)
        {
            if (ReferenceEquals(moved, container))
            {
                return true;
            }
        }

        return false;
    }

    // A container that is its own item belongs to that item and never serves another item, even when a stale
    // index still holds it.
    private static bool IsCompatibleContainer(UIElement container, object? item, Type containerType)
    {
        if (item is UIElement element && ReferenceEquals(containerType, element.GetType()))
        {
            return ReferenceEquals(container, element);
        }

        if (!ReferenceEquals(container, item) && ReferenceEquals(GetItem(container), container))
        {
            return false;
        }

        return containerType.IsAssignableFrom(container.GetType());
    }

    private static void DetachContainer(UIElement container)
    {
        while (container.VisualParent is UIElement visualParent)
        {
            if (!visualParent.VisualChildren.Remove(container))
            {
                break;
            }
        }

        while (container.LogicalParent is UIElement logicalParent)
        {
            if (!logicalParent.LogicalChildren.Remove(container))
            {
                break;
            }
        }
    }

    private sealed class ItemContainerInfo
    {
        public int Index { get; set; } = -1;

        public object? Item { get; set; }

        public bool IsSelected { get; set; }
    }
}

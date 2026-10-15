using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls.Items;

public sealed class ItemContainerGenerator
{
    private static readonly ConditionalWeakTable<UIElement, ItemContainerInfo> containerInfo = new();
    private readonly ItemsControl owner;
    private readonly Dictionary<int, UIElement> realized = [];

    // Containers that a realization without recycling took off their index, because the index now needs another
    // container. They stay attached and prepared for their previous items until the caller commits the window.
    private readonly List<UIElement> displaced = [];

    public ItemContainerGenerator(ItemsControl owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        RecyclePool = new ItemContainerRecyclePool();
    }

    public ItemContainerRecyclePool RecyclePool { get; }

    public IReadOnlyDictionary<int, UIElement> RealizedContainers => realized;

    public IReadOnlyList<UIElement> Realize(RealizationWindow? window = null)
    {
        return Realize(window, recycleOutsideWindow: true);
    }

    // The containers outside the window stay realized, attached, and prepared for their items, so a preparation
    // that throws, or a caller step that throws before it commits the window, leaves them as they were. A container
    // that an index of the window no longer takes, because its item needs another container or an item that is its
    // own container moves there, is not realized any more but stays attached and prepared too. The caller recycles
    // both with RecycleAllExcept once it commits the window, so they serve a later realization.
    internal IReadOnlyList<UIElement> RealizeWithoutRecycling(RealizationWindow? window)
    {
        return Realize(window, recycleOutsideWindow: false);
    }

    internal void RecycleAllExcept(IReadOnlyList<UIElement> containers)
    {
        HashSet<UIElement> kept = new(containers, ReferenceEqualityComparer.Instance);
        RecycleDisplacedExcept(kept);
        foreach ((int index, UIElement container) in realized.ToArray())
        {
            if (!kept.Contains(container))
            {
                Recycle(index);
            }
        }
    }

    private IReadOnlyList<UIElement> Realize(RealizationWindow? window, bool recycleOutsideWindow)
    {
        int count = owner.ViewItemCount;
        int start = window?.StartIndex ?? 0;
        int end = window?.EndIndexExclusive ?? count;
        start = Math.Clamp(start, 0, count);
        end = Math.Clamp(end, start, count);
        HashSet<int> desired = [.. Enumerable.Range(start, end - start)
            .Select(owner.GetSourceIndexForViewIndex)];

        MoveShiftedItemContainers(desired, recycleDisplaced: recycleOutsideWindow);
        if (recycleOutsideWindow)
        {
            foreach (int index in realized.Keys.Where(index => !desired.Contains(index)).ToArray())
            {
                Recycle(index);
            }
        }

        List<UIElement> containers = [];
        for (int viewIndex = start; viewIndex < end; viewIndex++)
        {
            containers.Add(GetOrCreate(owner.GetSourceIndexForViewIndex(viewIndex), recycleDisplaced: recycleOutsideWindow));
        }

        if (recycleOutsideWindow)
        {
            RecycleDisplacedExcept(new HashSet<UIElement>(containers, ReferenceEqualityComparer.Instance));
        }

        return containers;
    }

    public UIElement GetOrCreate(int index)
    {
        return GetOrCreate(index, recycleDisplaced: true);
    }

    private UIElement GetOrCreate(int index, bool recycleDisplaced)
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
                // The selection can change while the container still shows a replaced item, which a selection
                // change does not prepare, so a recorded selection that differs from the owner's is stale too.
                if (!ReferenceEquals(GetItem(existing), item) ||
                    GetItemIndex(existing) != index ||
                    GetIsSelected(existing) != owner.IsItemSelected(index))
                {
                    PrepareRealizedContainer(existing, index, item);
                }

                return existing;
            }

            Displace(index, recycleDisplaced);
        }

        // An item that is its own container and is still realized under an index that no longer holds it moves to
        // this index, as Realize moves it, so one container is never realized under two indexes. It still shows its
        // item, so it is not cleared or detached.
        int movedFromIndex = GetMovedOwnContainerKey(index, item);
        if (movedFromIndex >= 0)
        {
            realized.Remove(movedFromIndex, out UIElement? moved);
            realized.Add(index, moved!);
            PrepareRealizedContainer(moved!, index, item);
            return moved!;
        }

        // An item that is its own container is never wrapped in a recycled container of the container type. Only
        // CreateItemContainer knows whether a subclass returns such an item, so it is asked before the pool.
        UIElement container = IsOfContainerType(item, containerType)
            ? CreateOwnContainer(index, item, containerType)
            : RecyclePool.Pop(containerType) ?? owner.CreateItemContainer(index, item);
        try
        {
            owner.PrepareItemContainer(container, index, item);
        }
        catch
        {
            // A container whose preparation throws is not realized, so it is cleared here like a recycled one
            // and a generated container goes back to the pool, where the next realization can reuse it. Preparation
            // can throw before it records the item, and the owner's clear tells an item that is its own container,
            // whose content it keeps, from a generated one by the recorded item, so the item is recorded first.
            SetInfo(container, index, item, isSelected: false);
            ReleaseContainer(container);
            throw;
        }

        realized.Add(index, container);
        return container;
    }

    public void Recycle(int index)
    {
        if (!realized.Remove(index, out UIElement? container))
        {
            return;
        }

        DetachContainer(container);
        ReleaseContainer(container);
    }

    // The container realized at index gives the index to another container. Without recycling it stays attached and
    // prepared for its item, so a realization that throws later leaves it where the panel presents it.
    private void Displace(int index, bool recycle)
    {
        if (recycle)
        {
            Recycle(index);
            return;
        }

        if (realized.Remove(index, out UIElement? container) && !displaced.Contains(container))
        {
            displaced.Add(container);
        }
    }

    // A displaced container that a later realization realized again, such as an item that is its own container, is
    // not displaced any more. Each container leaves the list before it is released, so a release that throws keeps the
    // rest for a later commit.
    private void RecycleDisplacedExcept(HashSet<UIElement> kept)
    {
        while (displaced.Count > 0)
        {
            UIElement container = displaced[0];
            displaced.RemoveAt(0);
            if (!kept.Contains(container) && GetRealizedKey(container) < 0)
            {
                DetachContainer(container);
                ReleaseContainer(container);
            }
        }
    }

    // A container that the presenter's panel did not take is given back without detaching it, because a parent it
    // has then is not the panel, for example another element that still holds an item that is its own container.
    internal void ReleaseUnpresented(UIElement container)
    {
        int index = GetRealizedKey(container);
        if (index < 0)
        {
            return;
        }

        realized.Remove(index);
        ReleaseContainer(container);
    }

    // A container that is its own item belongs to that item, so only a generated container goes to the pool.
    private void ReleaseContainer(UIElement container)
    {
        object? item = GetItem(container);
        owner.ClearItemContainer(container);
        if (!ReferenceEquals(container, item))
        {
            RecyclePool.Push(container);
        }
    }

    public void Clear()
    {
        RecycleDisplacedExcept([]);
        foreach (int index in realized.Keys.ToArray())
        {
            Recycle(index);
        }
    }

    // The owner reports each item change whose position is known, so realized containers stay keyed by the
    // current index of their item. A container for a replaced item keeps its index and is prepared for the new
    // item by the next realization; a container that moved to another index is prepared for that index then.
    // A container that wraps a replaced UIElement item is recycled now instead, so that element is free to be
    // wrapped by the container at its new index, for example when two items are swapped.
    internal void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        int replacedEnd = index + Math.Min(removedCount, addedCount);
        for (int removedIndex = index; removedIndex < index + removedCount; removedIndex++)
        {
            if (removedIndex >= replacedEnd || WrapsElementItem(removedIndex))
            {
                Recycle(removedIndex);
            }
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

    private bool WrapsElementItem(int index)
    {
        return realized.TryGetValue(index, out UIElement? container) &&
            GetItem(container) is UIElement item &&
            !ReferenceEquals(container, item);
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

    // The index of the item the container shows now, or -1. Item changes re-key realized containers at once,
    // while a moved container's recorded index changes only when it is prepared again. A container whose item
    // was replaced still shows the old item until the next realization prepares it, so it represents no index
    // until then. ItemCollection stores an equal item without a change, so a container holding an equal item
    // still shows the item at its index. A container whose preparation threw has recorded index -1 and can have
    // recorded an item it does not show, so it represents no index until a later realization prepares it.
    internal int IndexFromContainer(UIElement container)
    {
        int index = GetRealizedKey(container);
        return index >= 0 &&
            GetItemIndex(container) >= 0 &&
            Equals(GetItem(container), owner.GetItemAt(index))
            ? index
            : -1;
    }

    // The key the container is realized under, or -1, whatever item that key holds now.
    private int GetRealizedKey(UIElement container)
    {
        foreach ((int index, UIElement realizedContainer) in realized)
        {
            if (ReferenceEquals(realizedContainer, container))
            {
                return index;
            }
        }

        return -1;
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
    // It is prepared at its new index here, because its recorded index can already equal the new index while its
    // other prepared state, such as its selection, is stale.
    private void MoveShiftedItemContainers(HashSet<int> desired, bool recycleDisplaced)
    {
        List<(int Index, int CurrentIndex, UIElement Container)>? moves = null;
        foreach (int index in desired)
        {
            int currentIndex = GetMovedOwnContainerKey(index, owner.GetItemAt(index));
            if (currentIndex < 0)
            {
                continue;
            }

            UIElement element = realized[currentIndex];
            if (ContainsMovedContainer(moves, element))
            {
                continue;
            }

            (moves ??= []).Add((index, currentIndex, element));
        }

        if (moves is null)
        {
            return;
        }

        // The containers that the moves displace give up their indexes one at a time while every moved container is
        // still realized under its previous index, so an owner's clear that throws for a displaced container leaves
        // the moved containers realized there, and a later realization moves them.
        foreach ((int index, _, _) in moves)
        {
            if (realized.TryGetValue(index, out UIElement? occupant) && !ContainsMovedContainer(moves, occupant))
            {
                Displace(index, recycleDisplaced);
            }
        }

        foreach ((_, int currentIndex, _) in moves)
        {
            realized.Remove(currentIndex);
        }

        // Every moved container is keyed by its new index before any of them is prepared, so a preparation that
        // throws leaves the others realized there, and a later realization prepares them like any realized container.
        foreach ((int index, _, UIElement container) in moves)
        {
            realized.Add(index, container);
        }

        foreach ((int index, _, UIElement container) in moves)
        {
            PrepareRealizedContainer(container, index, container);
        }
    }

    // The key under which the item at index, when it is its own container, is still realized although that key no
    // longer holds the item, or -1. The container is found by its realized key, because item changes re-key it
    // without changing its recorded index.
    private int GetMovedOwnContainerKey(int index, object? item)
    {
        if (item is not UIElement element || !ReferenceEquals(GetItem(element), element))
        {
            return -1;
        }

        int currentIndex = GetRealizedKey(element);
        return currentIndex == index ||
            (currentIndex >= 0 && currentIndex < owner.ItemCount &&
                ReferenceEquals(owner.GetItemAt(currentIndex), element))
            ? -1
            : currentIndex;
    }

    // A realized container stays realized when its preparation throws, but the preparation can have recorded the
    // new item, index, and selection before it threw. Its recorded index is reset, so the next realization prepares
    // it again instead of treating the preparation that did not finish as done. The recorded item is kept, because
    // it tells a container that is its own item from a generated one. A selector prepares its realized containers
    // through this method too when their selection changes.
    internal void PrepareRealizedContainer(UIElement container, int index, object? item)
    {
        try
        {
            owner.PrepareItemContainer(container, index, item);
        }
        catch
        {
            SetInfo(container, -1, GetItem(container), GetIsSelected(container));
            throw;
        }
    }

    private static bool ContainsMovedContainer(List<(int Index, int CurrentIndex, UIElement Container)>? moves, UIElement container)
    {
        if (moves is null)
        {
            return false;
        }

        foreach ((_, _, UIElement moved) in moves)
        {
            if (ReferenceEquals(moved, container))
            {
                return true;
            }
        }

        return false;
    }

    // A container that is its own item belongs to that item and never serves another item, even when a stale
    // index still holds it. A container that still shows the item is the one CreateItemContainer chose for it,
    // whatever its type, because a change of the owner's item policy clears every realized container. It is kept,
    // so it is not recycled and created again.
    private bool IsCompatibleContainer(UIElement container, object? item, Type containerType)
    {
        if (IsItsOwnContainer(item, containerType))
        {
            return ReferenceEquals(container, item) || ReferenceEquals(GetItem(container), item);
        }

        if (!ReferenceEquals(container, item) && ReferenceEquals(GetItem(container), container))
        {
            return false;
        }

        if (ReferenceEquals(GetItem(container), item))
        {
            return true;
        }

        // A container that shows another item is not prepared for an item of the container type, because a
        // subclass's CreateItemContainer can return that item itself.
        if (!ReferenceEquals(container, item) && IsOfContainerType(item, containerType))
        {
            return false;
        }

        return containerType.IsAssignableFrom(container.GetType());
    }

    // The item is its own container only when it is of the container type and the owner says its
    // CreateItemContainer returns the item. A subclass that wraps UIElement items in another container type
    // through the overridable members keeps reusing and recycling those wrappers.
    private bool IsItsOwnContainer(object? item, Type containerType)
    {
        return IsOfContainerType(item, containerType) && owner.IsItemItsOwnContainer(item);
    }

    // An element item of the container type or of a type derived from it, such as a ListBoxItem subclass when a
    // ListBox subclass returns typeof(ListBoxItem), can be the container that CreateItemContainer returns.
    private static bool IsOfContainerType(object? item, Type containerType)
    {
        return item is UIElement element && containerType.IsAssignableFrom(element.GetType());
    }

    // Only CreateItemContainer knows whether the owner returns the item itself. A subclass that overrides it can
    // still wrap such an item in a container of the item's own type; that wrapper then comes from the recycle pool
    // like any other container, so the pool does not grow on every realization.
    private UIElement CreateOwnContainer(int index, object? item, Type containerType)
    {
        UIElement created = owner.CreateItemContainer(index, item);
        return ReferenceEquals(created, item) ? created : RecyclePool.Pop(containerType) ?? created;
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

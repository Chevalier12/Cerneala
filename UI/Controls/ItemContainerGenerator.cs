using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls;

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
        int count = owner.ItemCount;
        int start = window?.StartIndex ?? 0;
        int end = window?.EndIndexExclusive ?? count;
        start = Math.Clamp(start, 0, count);
        end = Math.Clamp(end, start, count);
        HashSet<int> desired = [.. Enumerable.Range(start, end - start)];

        foreach (int index in realized.Keys.Where(index => !desired.Contains(index)).ToArray())
        {
            Recycle(index);
        }

        List<UIElement> containers = [];
        for (int index = start; index < end; index++)
        {
            containers.Add(GetOrCreate(index));
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
                owner.PrepareItemContainer(existing, index, item);
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

    private static bool IsCompatibleContainer(UIElement container, object? item, Type containerType)
    {
        return item is UIElement element
            ? ReferenceEquals(container, element)
            : containerType.IsAssignableFrom(container.GetType());
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

using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Layout.Panels;

public class Panel : UIElement
{
    public static readonly UiProperty<int> ZIndexProperty = UiProperty<int>.Register(
        "ZIndex",
        typeof(Panel),
        new UiPropertyMetadata<int>(
            0,
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsSemantics));

    private UIElement[]? zOrder;
    private long[]? zKeys;
    private bool isZOrderCurrent;
    private int zOrderChildrenVersion;

    public Panel()
    {
        Children = new PanelChildCollection(this);
    }

    public PanelChildCollection Children { get; }

    public static int GetZIndex(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(ZIndexProperty);
    }

    public static void SetZIndex(UIElement element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ZIndexProperty, value);
    }

    // The panel measures, arranges and draws only its visual children, so a child that is only a
    // logical child would be ignored without any error.
    internal override void ValidateChildInsertion(UIElement child, ElementChildRole role, bool ownerManaged)
    {
        base.ValidateChildInsertion(child, role, ownerManaged);
        if (role == ElementChildRole.Logical && !ownerManaged && !ReferenceEquals(child.VisualParent, this))
        {
            throw new InvalidOperationException(
                "A panel lays out and draws only its visual children, so a child added only to LogicalChildren would never be shown. " +
                "Add it with Panel.Children.Add, which adds it to LogicalChildren and VisualChildren together.");
        }
    }

    internal override void InvalidateVisualChildZOrder()
    {
        isZOrderCurrent = false;
    }

    internal override UIElement GetVisualChildInZOrder(int index)
    {
        if (!isZOrderCurrent || zOrderChildrenVersion != VisualChildren.Version)
        {
            RebuildZOrder();
        }

        return zOrder is null ? VisualChildren[index] : zOrder[index];
    }

    // Children order when the ZIndex values never decrease along it (zOrder stays null and nothing is allocated);
    // otherwise a stable sort by (ZIndex, index in VisualChildren). The arrays are reused while the count is the same.
    private void RebuildZOrder()
    {
        UIElementCollection children = VisualChildren;
        int count = children.Count;
        bool isOrdered = true;
        int previous = int.MinValue;
        for (int i = 0; i < count; i++)
        {
            int zIndex = GetZIndex(children[i]);
            if (zIndex < previous)
            {
                isOrdered = false;
                break;
            }

            previous = zIndex;
        }

        if (isOrdered)
        {
            zOrder = null;
            zKeys = null;
        }
        else
        {
            UIElement[] items = zOrder is { } oldItems && oldItems.Length == count ? oldItems : new UIElement[count];
            long[] keys = zKeys is { } oldKeys && oldKeys.Length == count ? oldKeys : new long[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = children[i];
                keys[i] = ((long)GetZIndex(items[i]) << 32) | (uint)i;
            }

            Array.Sort(keys, items);
            zOrder = items;
            zKeys = keys;
        }

        isZOrderCurrent = true;
        zOrderChildrenVersion = children.Version;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        float width = 0;
        float height = 0;
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            LayoutSize childSize = child.Measure(context);
            width = MathF.Max(width, childSize.Width);
            height = MathF.Max(height, childSize.Height);
        }

        return new LayoutSize(width, height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            child.Arrange(new ArrangeContext(context.FinalRect, context.Rounding));
        }

        return context.FinalRect;
    }
}

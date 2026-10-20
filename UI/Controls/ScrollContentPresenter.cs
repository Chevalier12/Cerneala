using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class ScrollContentPresenter : ContentControl, IScrollInfo
{
    public static readonly UiProperty<float> HorizontalOffsetProperty = UiProperty<float>.Register(
        nameof(HorizontalOffset),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest,
            validateValue: IsValidOffset,
            coerceValue: CoerceHorizontalOffset));

    public static readonly UiProperty<float> VerticalOffsetProperty = UiProperty<float>.Register(
        nameof(VerticalOffset),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest,
            validateValue: IsValidOffset,
            coerceValue: CoerceVerticalOffset));

    public static readonly UiProperty<float> ExtentWidthProperty = UiProperty<float>.Register(
        nameof(ExtentWidth),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.None, validateValue: IsValidExtent));

    public static readonly UiProperty<float> ExtentHeightProperty = UiProperty<float>.Register(
        nameof(ExtentHeight),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.None, validateValue: IsValidExtent));

    public static readonly UiProperty<float> ViewportWidthProperty = UiProperty<float>.Register(
        nameof(ViewportWidth),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.None, validateValue: IsValidExtent));

    public static readonly UiProperty<float> ViewportHeightProperty = UiProperty<float>.Register(
        nameof(ViewportHeight),
        typeof(ScrollContentPresenter),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.None, validateValue: IsValidExtent));

    private bool canHorizontallyScroll = true;
    private bool canVerticallyScroll = true;
    private bool hostsItemsViewport;
    private bool? viewerMeasuredWithFiniteHeight;

    public float HorizontalOffset
    {
        get => GetValue(HorizontalOffsetProperty);
        private set => SetValue(HorizontalOffsetProperty, value);
    }

    public float VerticalOffset
    {
        get => GetValue(VerticalOffsetProperty);
        private set => SetValue(VerticalOffsetProperty, value);
    }

    public float ExtentWidth
    {
        get => GetValue(ExtentWidthProperty);
        private set => SetValue(ExtentWidthProperty, value);
    }

    public float ExtentHeight
    {
        get => GetValue(ExtentHeightProperty);
        private set => SetValue(ExtentHeightProperty, value);
    }

    public float ViewportWidth
    {
        get => GetValue(ViewportWidthProperty);
        private set => SetValue(ViewportWidthProperty, value);
    }

    public float ViewportHeight
    {
        get => GetValue(ViewportHeightProperty);
        private set => SetValue(ViewportHeightProperty, value);
    }

    // MeasureCore reads both capabilities to choose the content constraint, so a change invalidates the measure, as in
    // WPF. ScrollViewer measures this presenter with the same size after a switch between Disabled and Auto, and the
    // cached measure would keep the old constraint (#561).
    public bool CanHorizontallyScroll
    {
        get => canHorizontallyScroll;
        set
        {
            if (canHorizontallyScroll == value)
            {
                return;
            }

            canHorizontallyScroll = value;
            Invalidate(InvalidationFlags.Measure, "Scroll capability changed");
        }
    }

    public bool CanVerticallyScroll
    {
        get => canVerticallyScroll;
        set
        {
            bool changed = canVerticallyScroll != value;
            canVerticallyScroll = value;
            if (!value)
            {
                StopItemsVirtualization(ContentElement);
            }

            if (changed)
            {
                Invalidate(InvalidationFlags.Measure, "Scroll capability changed");
            }
        }
    }

    // Whether the ScrollViewer whose template holds this presenter was measured with a finite height; null outside a
    // ScrollViewer, where this presenter's own available height decides. MeasureCore reads it, so a change invalidates
    // the measure.
    internal bool? ViewerMeasuredWithFiniteHeight
    {
        get => viewerMeasuredWithFiniteHeight;
        set
        {
            if (viewerMeasuredWithFiniteHeight == value)
            {
                return;
            }

            viewerMeasuredWithFiniteHeight = value;
            Invalidate(InvalidationFlags.Measure, "Scroll viewer height bound changed");
        }
    }

    // The pre-rounding displacement applied by the last arrange, not the requested offset.
    // Layout snapshots can precede the arrange that applies a new offset.
    internal LayoutPoint ArrangedScrollOffset { get; private set; }

    public void SetHorizontalOffset(float offset)
    {
        HorizontalOffset = CanHorizontallyScroll ? offset : 0;
    }

    public void SetVerticalOffset(float offset)
    {
        VerticalOffset = CanVerticallyScroll ? offset : 0;
        UpdateItemsVirtualization(ViewportHeight);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        UIElement? content = ContentElement;
        LayoutSize available = context.AvailableSize;
        LayoutSize measureSize = new(
            CanHorizontallyScroll ? float.PositiveInfinity : available.Width,
            CanVerticallyScroll ? float.PositiveInfinity : available.Height);
        // An unbounded height, such as for a ListBox without a fixed height inside an outer ScrollViewer, shows every
        // item, so an items presenter content gets no viewport from this presenter and the outer viewer's applies.
        hostsItemsViewport = CanVerticallyScroll && available.Height >= 0 && float.IsFinite(available.Height) &&
            ViewerMeasuredWithFiniteHeight != false;
        if (!hostsItemsViewport)
        {
            StopHostedItemsVirtualization(content);
        }

        UpdateItemsVirtualization(available.Height);
        LayoutSize contentSize = content?.Measure(new MeasureContext(measureSize, context.Rounding)) ?? LayoutSize.Zero;
        ExtentWidth = ResolveExtent(contentSize.Width, available.Width);
        ExtentHeight = ResolveExtent(contentSize.Height, available.Height);
        ViewportWidth = ResolveViewport(available.Width, ExtentWidth);
        ViewportHeight = ResolveViewport(available.Height, ExtentHeight);
        // As WPF's ScrollContentPresenter: the viewport is the available size, and the desired size on each axis is
        // the smaller of the viewport and the extent, so content that fits does not ask for the whole space.
        return new LayoutSize(MathF.Min(ViewportWidth, ExtentWidth), MathF.Min(ViewportHeight, ExtentHeight));
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ViewportWidth = MathF.Max(0, context.FinalRect.Width);
        ViewportHeight = MathF.Max(0, context.FinalRect.Height);
        float verticalOffset = VerticalOffset;
        CoerceOffsets();
        if (VerticalOffset != verticalOffset)
        {
            // Measure pushed the offset before the new extent existed; the items window must follow the clamp.
            UpdateItemsVirtualization(ViewportHeight);
        }

        ClipNode.SetClip(this, context.Rounding.Round(context.FinalRect));
        float contentWidth = CanHorizontallyScroll ? MathF.Max(ExtentWidth, ViewportWidth) : ViewportWidth;
        float contentHeight = CanVerticallyScroll ? MathF.Max(ExtentHeight, ViewportHeight) : ViewportHeight;
        UIElement? content = ContentElement;
        content?.Arrange(new ArrangeContext(
            new LayoutRect(
                context.FinalRect.X - HorizontalOffset,
                context.FinalRect.Y - VerticalOffset,
                contentWidth,
                contentHeight),
            context.Rounding));
        ArrangedScrollOffset = content is null ? LayoutPoint.Zero : new LayoutPoint(HorizontalOffset, VerticalOffset);
        return context.FinalRect;
    }

    protected override void OnDetached()
    {
        ClipNode.ClearClip(this);
        base.OnDetached();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ContentProperty) && !ReferenceEquals(args.OldValue, args.NewValue))
        {
            StopItemsVirtualization(args.OldValue as UIElement);
        }
    }

    // Only this presenter knows when it stops pushing the viewport to an items control or an items presenter, which
    // keeps the last viewport for a virtualizing panel that enters later, so the control drops that viewport here.
    // The next measure decides again whether a new content gets a host viewport, so an offset set before it pushes
    // none with the viewport height of the old content.
    private void StopItemsVirtualization(UIElement? content)
    {
        hostsItemsViewport = false;
        if (content is ItemsControl itemsControl)
        {
            itemsControl.ClearAutomaticVirtualization();
        }

        StopHostedItemsVirtualization(content);
    }

    private static void StopHostedItemsVirtualization(UIElement? content)
    {
        if (content is ItemsPresenter { ItemsOwner: { } owner } presenter)
        {
            owner.ClearHostedVirtualization(presenter);
        }
    }

    private void CoerceOffsets()
    {
        HorizontalOffset = HorizontalOffset;
        VerticalOffset = VerticalOffset;
    }

    // As WPF's ScrollContentPresenter, which scrolls the panel of an ItemsPresenter child, an items presenter content
    // realizes only the items in this viewport, and only while the last measure gave this presenter and its ScrollViewer
    // a finite height.
    private void UpdateItemsVirtualization(float viewportExtent)
    {
        if (!CanVerticallyScroll || viewportExtent < 0 || !float.IsFinite(viewportExtent))
        {
            return;
        }

        switch (ContentElement)
        {
            case ItemsControl itemsControl:
                itemsControl.UpdateAutomaticVirtualization(viewportExtent, VerticalOffset);
                break;
            case ItemsPresenter { ItemsOwner: { } owner } presenter when hostsItemsViewport:
                owner.UpdateHostedVirtualization(presenter, viewportExtent, VerticalOffset);
                break;
        }
    }

    private float MaxHorizontalOffset => MathF.Max(0, ExtentWidth - ViewportWidth);

    private float MaxVerticalOffset => MathF.Max(0, ExtentHeight - ViewportHeight);

    private static float CoerceHorizontalOffset(UiObject owner, float value)
    {
        ScrollContentPresenter presenter = (ScrollContentPresenter)owner;
        if (!presenter.CanHorizontallyScroll)
        {
            return 0;
        }

        return MathF.Min(MathF.Max(0, value), presenter.MaxHorizontalOffset);
    }

    private static float CoerceVerticalOffset(UiObject owner, float value)
    {
        ScrollContentPresenter presenter = (ScrollContentPresenter)owner;
        if (!presenter.CanVerticallyScroll)
        {
            return 0;
        }

        return MathF.Min(MathF.Max(0, value), presenter.MaxVerticalOffset);
    }

    private static float ResolveExtent(float desired, float available)
    {
        if (float.IsPositiveInfinity(desired))
        {
            return float.IsPositiveInfinity(available) ? 0 : MathF.Max(0, available);
        }

        return MathF.Max(0, desired);
    }

    private static float ResolveViewport(float available, float extent)
    {
        return float.IsPositiveInfinity(available) ? extent : MathF.Max(0, available);
    }

    private static bool IsValidOffset(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }

    private static bool IsValidExtent(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }
}

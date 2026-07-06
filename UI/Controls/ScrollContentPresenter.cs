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

    public bool CanHorizontallyScroll { get; set; } = true;

    public bool CanVerticallyScroll { get; set; } = true;

    public void SetHorizontalOffset(float offset)
    {
        HorizontalOffset = CanHorizontallyScroll ? offset : 0;
    }

    public void SetVerticalOffset(float offset)
    {
        VerticalOffset = CanVerticallyScroll ? offset : 0;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        UIElement? content = ContentElement;
        LayoutSize available = context.AvailableSize;
        LayoutSize measureSize = new(
            CanHorizontallyScroll ? float.PositiveInfinity : available.Width,
            CanVerticallyScroll ? float.PositiveInfinity : available.Height);
        LayoutSize contentSize = content?.Measure(new MeasureContext(measureSize, context.Rounding)) ?? LayoutSize.Zero;
        ExtentWidth = ResolveExtent(contentSize.Width, available.Width);
        ExtentHeight = ResolveExtent(contentSize.Height, available.Height);
        ViewportWidth = ResolveViewport(available.Width, ExtentWidth);
        ViewportHeight = ResolveViewport(available.Height, ExtentHeight);
        CoerceOffsets();
        return new LayoutSize(ViewportWidth, ViewportHeight);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ViewportWidth = MathF.Max(0, context.FinalRect.Width);
        ViewportHeight = MathF.Max(0, context.FinalRect.Height);
        CoerceOffsets();
        ClipNode.SetClip(this, context.FinalRect);
        ContentElement?.Arrange(new ArrangeContext(
            new LayoutRect(
                context.FinalRect.X - HorizontalOffset,
                context.FinalRect.Y - VerticalOffset,
                MathF.Max(ExtentWidth, ViewportWidth),
                MathF.Max(ExtentHeight, ViewportHeight)),
            context.Rounding));
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ExtentWidthProperty) ||
            ReferenceEquals(args.Property, ExtentHeightProperty) ||
            ReferenceEquals(args.Property, ViewportWidthProperty) ||
            ReferenceEquals(args.Property, ViewportHeightProperty))
        {
            CoerceOffsets();
        }
    }

    protected override void OnDetached()
    {
        ClipNode.ClearClip(this);
        base.OnDetached();
    }

    private void CoerceOffsets()
    {
        HorizontalOffset = HorizontalOffset;
        VerticalOffset = VerticalOffset;
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

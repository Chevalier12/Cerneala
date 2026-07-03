using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Elements;

public class UIElement : UiObject, IUiPropertyOwner, ILayoutElement, IRenderableElement
{
    public static readonly UiProperty<bool> IsEnabledProperty = UiProperty<bool>.Register(
        nameof(IsEnabled),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<bool> IsVisibleProperty = UiProperty<bool>.Register(
        nameof(IsVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<Thickness> MarginProperty = UiProperty<Thickness>.Register(
        nameof(Margin),
        typeof(UIElement),
        new UiPropertyMetadata<Thickness>(Thickness.Zero, UiPropertyOptions.AffectsMeasure));

    public static readonly UiProperty<HorizontalAlignment> HorizontalAlignmentProperty = UiProperty<HorizontalAlignment>.Register(
        nameof(HorizontalAlignment),
        typeof(UIElement),
        new UiPropertyMetadata<HorizontalAlignment>(HorizontalAlignment.Stretch, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<VerticalAlignment> VerticalAlignmentProperty = UiProperty<VerticalAlignment>.Register(
        nameof(VerticalAlignment),
        typeof(UIElement),
        new UiPropertyMetadata<VerticalAlignment>(VerticalAlignment.Stretch, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<Visibility> VisibilityProperty = UiProperty<Visibility>.Register(
        nameof(Visibility),
        typeof(UIElement),
        new UiPropertyMetadata<Visibility>(
            Visibility.Visible,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<bool> IsPointerOverProperty = UiProperty<bool>.Register(
        nameof(IsPointerOver),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<bool> IsKeyboardFocusedProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocused),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<bool> IsKeyboardFocusWithinProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocusWithin),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle));

    public UIElement()
    {
        LogicalChildren = new UIElementCollection(this, ElementChildRole.Logical);
        VisualChildren = new UIElementCollection(this, ElementChildRole.Visual);
        Handlers = new ElementHandlerStore(this);
    }

    public UIElement? LogicalParent { get; private set; }

    public UIElement? VisualParent { get; private set; }

    public UIElementCollection LogicalChildren { get; }

    public UIElementCollection VisualChildren { get; }

    public UIRoot? Root { get; private set; }

    public bool IsAttached => Root is not null;

    public UiElementId? ElementId { get; private set; }

    public ElementHandlerStore Handlers { get; }

    public CommandBindingCollection CommandBindings { get; } = new();

    public DirtyState DirtyState { get; } = new();

    public LayoutSize DesiredSize { get; private set; }

    public LayoutRect ArrangedBounds { get; private set; }

    public int LayoutVersion { get; private set; }

    public int RenderVersion { get; private set; }

    public RenderDependency RenderDependencies { get; private set; }

    public bool IsLayoutBoundary { get; set; }

    internal LayoutSize? LastMeasureAvailableSize { get; set; }

    internal int LastMeasureLayoutVersion { get; set; } = -1;

    internal LayoutRect? LastArrangeFinalRect { get; set; }

    internal int LastArrangeLayoutVersion { get; set; } = -1;

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public Thickness Margin
    {
        get => GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => GetValue(HorizontalAlignmentProperty);
        set => SetValue(HorizontalAlignmentProperty, value);
    }

    public VerticalAlignment VerticalAlignment
    {
        get => GetValue(VerticalAlignmentProperty);
        set => SetValue(VerticalAlignmentProperty, value);
    }

    public Visibility Visibility
    {
        get => GetValue(VisibilityProperty);
        set => SetValue(VisibilityProperty, value);
    }

    public bool IsPointerOver
    {
        get => GetValue(IsPointerOverProperty);
        set => SetValue(IsPointerOverProperty, value);
    }

    public bool IsKeyboardFocused
    {
        get => GetValue(IsKeyboardFocusedProperty);
        set => SetValue(IsKeyboardFocusedProperty, value);
    }

    public bool IsKeyboardFocusWithin
    {
        get => GetValue(IsKeyboardFocusWithinProperty);
        set => SetValue(IsKeyboardFocusWithinProperty, value);
    }

    internal bool HasAttachedParent =>
        (LogicalParent?.Root is not null) || (VisualParent?.Root is not null);

    internal void SetLogicalParent(UIElement? parent)
    {
        LogicalParent = parent;
    }

    internal void SetVisualParent(UIElement? parent)
    {
        VisualParent = parent;
    }

    internal void AttachToRoot(UIRoot root, UiElementId id)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        ElementId = id;
        OnAttached();
    }

    internal void DetachFromRoot()
    {
        OnDetached();
        ElementId = null;
        Root = null;
    }

    protected virtual void OnAttached()
    {
    }

    protected virtual void OnDetached()
    {
    }

    public LayoutSize Measure(MeasureContext context)
    {
        if (LastMeasureAvailableSize == context.AvailableSize &&
            LastMeasureLayoutVersion == LayoutVersion)
        {
            return DesiredSize;
        }

        LayoutSize desired = Visibility == Visibility.Collapsed
            ? LayoutSize.Zero
            : MeasureCore(context).ClampNonNegative();
        desired = context.Rounding.Round(desired);
        SetDesiredSize(desired);
        LastMeasureAvailableSize = context.AvailableSize;
        LastMeasureLayoutVersion = LayoutVersion;
        return desired;
    }

    public LayoutRect Arrange(ArrangeContext context)
    {
        if (LastArrangeFinalRect == context.FinalRect &&
            LastArrangeLayoutVersion == LayoutVersion)
        {
            return ArrangedBounds;
        }

        LayoutRect finalRect = Visibility == Visibility.Collapsed
            ? context.FinalRect
            : ApplyAlignment(context.FinalRect);

        LayoutRect arranged = Visibility == Visibility.Collapsed
            ? new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0)
            : ArrangeCore(new ArrangeContext(finalRect, context.Rounding));
        arranged = context.Rounding.Round(arranged);
        SetArrangedBounds(arranged);
        LastArrangeFinalRect = context.FinalRect;
        LastArrangeLayoutVersion = LayoutVersion;
        return arranged;
    }

    protected virtual LayoutSize MeasureCore(MeasureContext context)
    {
        return LayoutSize.Zero;
    }

    protected virtual LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }

    public void Render(RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        OnRender(context);
    }

    protected virtual void OnRender(RenderContext context)
    {
    }

    internal void SetDesiredSize(LayoutSize desiredSize)
    {
        DesiredSize = desiredSize;
    }

    internal bool SetArrangedBounds(LayoutRect arrangedBounds)
    {
        if (ArrangedBounds == arrangedBounds)
        {
            return false;
        }

        ArrangedBounds = arrangedBounds;
        IncrementRenderVersion();
        Root?.RetainedRenderCache.InvalidateRoot();
        return true;
    }

    internal void IncrementLayoutVersion()
    {
        LayoutVersion++;
    }

    internal void IncrementRenderVersion()
    {
        RenderVersion++;
    }

    protected void SetRenderDependencies(RenderDependency dependencies)
    {
        if (RenderDependencies == dependencies)
        {
            return;
        }

        RenderDependencies = dependencies;
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "Render dependencies changed");
    }

    private LayoutRect ApplyAlignment(LayoutRect finalRect)
    {
        float width = HorizontalAlignment == HorizontalAlignment.Stretch
            ? finalRect.Width
            : Math.Min(DesiredSize.Width, finalRect.Width);
        float height = VerticalAlignment == VerticalAlignment.Stretch
            ? finalRect.Height
            : Math.Min(DesiredSize.Height, finalRect.Height);

        float x = HorizontalAlignment switch
        {
            HorizontalAlignment.Center => finalRect.X + ((finalRect.Width - width) / 2),
            HorizontalAlignment.Right => finalRect.X + finalRect.Width - width,
            _ => finalRect.X
        };

        float y = VerticalAlignment switch
        {
            VerticalAlignment.Center => finalRect.Y + ((finalRect.Height - height) / 2),
            VerticalAlignment.Bottom => finalRect.Y + finalRect.Height - height,
            _ => finalRect.Y
        };

        return new LayoutRect(x, y, width, height);
    }

    public void Invalidate(InvalidationFlags flags, string reason)
    {
        Invalidate(new InvalidationRequest(this, flags, reason));
    }

    public virtual void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ReferenceEquals(request.Target, this))
        {
            throw new InvalidOperationException("Invalidation request target must match the element.");
        }

        if (Root is null)
        {
            DirtyState.Mark(DirtyPropagation.Default.GetEffectiveFlags(request));
            return;
        }

        Root.Invalidate(request);
    }

    public void OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options)
    {
        ArgumentNullException.ThrowIfNull(args);
        if ((options & (UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange)) != UiPropertyOptions.None)
        {
            IncrementLayoutVersion();
        }

        if ((options & (UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual)) != UiPropertyOptions.None)
        {
            IncrementRenderVersion();
        }

        InvalidationFlags flags = MapInvalidationOptions(options);
        if (flags != InvalidationFlags.None)
        {
            Invalidate(new InvalidationRequest(this, flags, "Property changed", args.Property));
        }
    }

    private static InvalidationFlags MapInvalidationOptions(UiPropertyOptions options)
    {
        InvalidationFlags flags = InvalidationFlags.None;
        if (options.HasFlag(UiPropertyOptions.AffectsMeasure))
        {
            flags |= InvalidationFlags.Measure;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsArrange))
        {
            flags |= InvalidationFlags.Arrange;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsRender))
        {
            flags |= InvalidationFlags.Render;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsHitTest))
        {
            flags |= InvalidationFlags.HitTest;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsStyle))
        {
            flags |= InvalidationFlags.Style;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsInputVisual))
        {
            flags |= InvalidationFlags.InputVisual;
        }

        return flags;
    }
}

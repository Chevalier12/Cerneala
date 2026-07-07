using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Elements;

public class UIElement : UiObject, IUiPropertyOwner, ILayoutElement, IRenderableElement
{
    public static readonly UiProperty<bool> IsEnabledProperty = UiProperty<bool>.Register(
        nameof(IsEnabled),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsVisibleProperty = UiProperty<bool>.Register(
        nameof(IsVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsSemantics));

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
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<Transform> RenderTransformProperty = UiProperty<Transform>.Register(
        nameof(RenderTransform),
        typeof(UIElement),
        new UiPropertyMetadata<Transform>(Transform.Identity, UiPropertyOptions.AffectsRender, validateValue: value => value is not null));

    public static readonly UiProperty<LayoutPoint> RenderTransformOriginProperty = UiProperty<LayoutPoint>.Register(
        nameof(RenderTransformOrigin),
        typeof(UIElement),
        new UiPropertyMetadata<LayoutPoint>(new LayoutPoint(0.5f, 0.5f), UiPropertyOptions.AffectsRender, validateValue: IsValidNormalizedPoint));

    public static readonly UiProperty<float> OpacityProperty = UiProperty<float>.Register(
        nameof(Opacity),
        typeof(UIElement),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: value => float.IsFinite(value) && value >= 0 && value <= 1));

    public static readonly UiProperty<float> TranslateXProperty = UiProperty<float>.Register(
        nameof(TranslateX),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> TranslateYProperty = UiProperty<float>.Register(
        nameof(TranslateY),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> ScaleProperty = UiProperty<float>.Register(
        nameof(Scale),
        typeof(UIElement),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> ScaleXProperty = UiProperty<float>.Register(
        nameof(ScaleX),
        typeof(UIElement),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> ScaleYProperty = UiProperty<float>.Register(
        nameof(ScaleY),
        typeof(UIElement),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> RotationProperty = UiProperty<float>.Register(
        nameof(Rotation),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> SkewXProperty = UiProperty<float>.Register(
        nameof(SkewX),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> SkewYProperty = UiProperty<float>.Register(
        nameof(SkewY),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<bool> ClipToBoundsProperty = UiProperty<bool>.Register(
        nameof(ClipToBounds),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<LayoutMotionId?> LayoutMotionIdProperty = UiProperty<LayoutMotionId?>.Register(
        nameof(LayoutMotionId),
        typeof(UIElement),
        new UiPropertyMetadata<LayoutMotionId?>(null, UiPropertyOptions.None, validateValue: IsValidLayoutMotionId));

    public static readonly UiProperty<LayoutMotionOptions?> LayoutMotionOptionsProperty = UiProperty<LayoutMotionOptions?>.Register(
        nameof(LayoutMotion),
        typeof(UIElement),
        new UiPropertyMetadata<LayoutMotionOptions?>(null, UiPropertyOptions.None));

    public static readonly UiProperty<PresenceOptions?> PresenceProperty = UiProperty<PresenceOptions?>.Register(
        nameof(Presence),
        typeof(UIElement),
        new UiPropertyMetadata<PresenceOptions?>(null, UiPropertyOptions.None));

    public static readonly UiProperty<bool> IsPointerOverProperty = UiProperty<bool>.Register(
        nameof(IsPointerOver),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsKeyboardFocusedProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocused),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsKeyboardFocusWithinProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocusWithin),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> FocusableProperty = UiProperty<bool>.Register(
        nameof(Focusable),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<bool> IsTabStopProperty = UiProperty<bool>.Register(
        nameof(IsTabStop),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<int> TabIndexProperty = UiProperty<int>.Register(
        nameof(TabIndex),
        typeof(UIElement),
        new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsHitTest, validateValue: value => value >= 0));

    public static readonly UiProperty<Cursor?> CursorProperty = UiProperty<Cursor?>.Register(
        nameof(Cursor),
        typeof(UIElement),
        new UiPropertyMetadata<Cursor?>(null, UiPropertyOptions.None));

    public UIElement()
    {
        LogicalChildren = new UIElementCollection(this, ElementChildRole.Logical);
        VisualChildren = new UIElementCollection(this, ElementChildRole.Visual);
        Handlers = new ElementHandlerStore(this);
        CommandBindings = new CommandBindingCollection(this);
        Bindings = new BindingSubscriptionCollection();
    }

    public UIElement? LogicalParent { get; private set; }

    public UIElement? VisualParent { get; private set; }

    public UIElementCollection LogicalChildren { get; }

    public UIElementCollection VisualChildren { get; }

    public UIRoot? Root { get; private set; }

    public bool IsAttached => Root is not null;

    public UiElementId? ElementId { get; private set; }

    public ElementHandlerStore Handlers { get; }

    public CommandBindingCollection CommandBindings { get; }

    public BindingSubscriptionCollection Bindings { get; }

    public InputBindingCollection InputBindings { get; } = new();

    public DirtyState DirtyState { get; } = new();

    public LayoutSize DesiredSize { get; private set; }

    public LayoutRect ArrangedBounds { get; private set; }

    public int LayoutVersion { get; private set; }

    public int RenderVersion { get; private set; }

    public int RenderScopeVersion { get; private set; }

    public RenderDependency RenderDependencies { get; private set; }

    public bool IsLayoutBoundary { get; set; }

    internal Transform LayoutCorrectionTransform { get; private set; } = Transform.Identity;

    internal bool IsPresenceExiting { get; private set; }

    public float PresenceOpacity { get; private set; } = 1;

    public float PresenceScale { get; private set; } = 1;

    protected override UiPropertyMutationObserver? MutationObserver => Root?.Motion.Transactions;

    private bool hasPendingCommandStateRefresh;
    private bool hasPendingRenderScopeInvalidation;
    private bool hasPendingRenderContentInvalidation;

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

    public Transform RenderTransform
    {
        get => GetValue(RenderTransformProperty);
        set => SetValue(RenderTransformProperty, value);
    }

    public LayoutPoint RenderTransformOrigin
    {
        get => GetValue(RenderTransformOriginProperty);
        set => SetValue(RenderTransformOriginProperty, value);
    }

    public float Opacity
    {
        get => GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    public float TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public float TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    public float Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public float ScaleX
    {
        get => GetValue(ScaleXProperty);
        set => SetValue(ScaleXProperty, value);
    }

    public float ScaleY
    {
        get => GetValue(ScaleYProperty);
        set => SetValue(ScaleYProperty, value);
    }

    public float Rotation
    {
        get => GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }

    public float SkewX
    {
        get => GetValue(SkewXProperty);
        set => SetValue(SkewXProperty, value);
    }

    public float SkewY
    {
        get => GetValue(SkewYProperty);
        set => SetValue(SkewYProperty, value);
    }

    public bool ClipToBounds
    {
        get => GetValue(ClipToBoundsProperty);
        set => SetValue(ClipToBoundsProperty, value);
    }

    public LayoutMotionId? LayoutMotionId
    {
        get => GetValue(LayoutMotionIdProperty);
        set => SetValue(LayoutMotionIdProperty, value);
    }

    public LayoutMotionOptions? LayoutMotion
    {
        get => GetValue(LayoutMotionOptionsProperty);
        set => SetValue(LayoutMotionOptionsProperty, value);
    }

    public PresenceOptions? Presence
    {
        get => GetValue(PresenceProperty);
        set => SetValue(PresenceProperty, value);
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

    public bool Focusable
    {
        get => GetValue(FocusableProperty);
        set => SetValue(FocusableProperty, value);
    }

    public bool IsTabStop
    {
        get => GetValue(IsTabStopProperty);
        set => SetValue(IsTabStopProperty, value);
    }

    public int TabIndex
    {
        get => GetValue(TabIndexProperty);
        set => SetValue(TabIndexProperty, value);
    }

    public Cursor? Cursor
    {
        get => GetValue(CursorProperty);
        set => SetValue(CursorProperty, value);
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
        Root?.Motion.Presence.MarkAttached(this);
    }

    internal void DetachFromRoot()
    {
        Root?.Motion.Presence.MarkDetached(this);
        OnDetached();
        Bindings.Clear();
        ElementId = null;
        Root = null;
    }

    protected virtual void OnAttached()
    {
        if (hasPendingCommandStateRefresh)
        {
            hasPendingCommandStateRefresh = false;
            QueueCommandStateRefresh();
        }
    }

    protected virtual void OnDetached()
    {
    }

    public void QueueCommandStateRefresh()
    {
        if (this is not ICommandStateSource)
        {
            return;
        }

        if (Root is null)
        {
            hasPendingCommandStateRefresh = true;
            return;
        }

        hasPendingCommandStateRefresh = false;
        Root.CommandStateQueue.Enqueue(this);
    }

    internal void QueueDescendantCommandStateRefreshes()
    {
        foreach (UIElement element in ElementTreeWalker.PreOrder(this, ElementChildRole.Visual))
        {
            element.QueueCommandStateRefresh();
        }
    }

    public LayoutSize Measure(MeasureContext context)
    {
        Root?.CountMeasureCall();
        if (LastMeasureAvailableSize == context.AvailableSize &&
            LastMeasureLayoutVersion == LayoutVersion)
        {
            return DesiredSize;
        }

        LayoutSize desired = !UIElementVisibility.ParticipatesInLayout(this)
            ? LayoutSize.Zero
            : MeasureWithMargin(context);
        desired = context.Rounding.Round(desired);
        SetDesiredSize(desired);
        LastMeasureAvailableSize = context.AvailableSize;
        LastMeasureLayoutVersion = LayoutVersion;
        return desired;
    }

    public LayoutRect Arrange(ArrangeContext context)
    {
        Root?.CountArrangeCall();
        if (LastArrangeFinalRect == context.FinalRect &&
            LastArrangeLayoutVersion == LayoutVersion)
        {
            return ArrangedBounds;
        }

        LayoutRect finalRect = !UIElementVisibility.ParticipatesInLayout(this)
            ? context.FinalRect
            : ApplyMarginAndAlignment(context.FinalRect);

        LayoutRect arranged = !UIElementVisibility.ParticipatesInLayout(this)
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
        Root?.RenderQueue.Enqueue(this);
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

    internal bool ConsumeRenderScopeOnlyInvalidation()
    {
        bool isScopeOnly = hasPendingRenderScopeInvalidation && !hasPendingRenderContentInvalidation;
        hasPendingRenderScopeInvalidation = false;
        hasPendingRenderContentInvalidation = false;
        return isScopeOnly;
    }

    internal void SetLayoutCorrectionTransform(Transform correction)
    {
        ArgumentNullException.ThrowIfNull(correction);
        if (LayoutCorrectionTransform == correction)
        {
            return;
        }

        LayoutCorrectionTransform = correction;
        RenderScopeVersion++;
        hasPendingRenderScopeInvalidation = true;
        Invalidate(InvalidationFlags.Render, "Layout motion correction changed");
    }

    internal void SetPresenceExiting(bool isExiting)
    {
        if (IsPresenceExiting == isExiting)
        {
            return;
        }

        IsPresenceExiting = isExiting;
        Invalidate(InvalidationFlags.HitTest, "Presence state changed");
    }

    internal void SetPresenceVisual(float opacity, float scale)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        if (!float.IsFinite(scale))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Presence scale must be finite.");
        }

        if (PresenceOpacity == opacity && PresenceScale == scale)
        {
            return;
        }

        PresenceOpacity = opacity;
        PresenceScale = scale;
        RenderScopeVersion++;
        hasPendingRenderScopeInvalidation = true;
        Invalidate(InvalidationFlags.Render, "Presence visual state changed");
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

    private LayoutSize MeasureWithMargin(MeasureContext context)
    {
        Thickness margin = Margin;
        LayoutSize contentAvailableSize = Deflate(context.AvailableSize, margin);
        LayoutSize contentDesiredSize = MeasureCore(new MeasureContext(contentAvailableSize, context.Rounding)).ClampNonNegative();
        return new LayoutSize(
            contentDesiredSize.Width + margin.Horizontal,
            contentDesiredSize.Height + margin.Vertical).ClampNonNegative();
    }

    private LayoutRect ApplyMarginAndAlignment(LayoutRect finalRect)
    {
        Thickness margin = Margin;
        LayoutRect contentRect = new(
            finalRect.X + margin.Left,
            finalRect.Y + margin.Top,
            MathF.Max(0, finalRect.Width - margin.Horizontal),
            MathF.Max(0, finalRect.Height - margin.Vertical));
        LayoutSize contentDesiredSize = Deflate(DesiredSize, margin);
        return ApplyAlignment(contentRect, contentDesiredSize);
    }

    private static LayoutSize Deflate(LayoutSize size, Thickness thickness)
    {
        return new LayoutSize(
            MathF.Max(0, size.Width - thickness.Horizontal),
            MathF.Max(0, size.Height - thickness.Vertical));
    }

    private LayoutRect ApplyAlignment(LayoutRect finalRect, LayoutSize desiredSize)
    {
        float width = HorizontalAlignment == HorizontalAlignment.Stretch
            ? finalRect.Width
            : Math.Min(desiredSize.Width, finalRect.Width);
        float height = VerticalAlignment == VerticalAlignment.Stretch
            ? finalRect.Height
            : Math.Min(desiredSize.Height, finalRect.Height);

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
            InvalidationFlags effective = DirtyPropagation.Default.GetEffectiveFlags(request);
            DirtyState.Mark(effective);
            if ((effective & (InvalidationFlags.Measure | InvalidationFlags.Arrange)) != InvalidationFlags.None)
            {
                IncrementLayoutVersion();
            }

            if (effective.HasFlag(InvalidationFlags.Measure))
            {
                foreach (UIElement ancestor in ElementTreeWalker.Ancestors(this, ElementChildRole.Visual))
                {
                    ancestor.DirtyState.Mark(InvalidationFlags.Measure | InvalidationFlags.Arrange);
                    ancestor.IncrementLayoutVersion();
                    if (ancestor.IsLayoutBoundary)
                    {
                        break;
                    }
                }
            }

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
            if (IsRenderScopeProperty(args.Property))
            {
                RenderScopeVersion++;
                hasPendingRenderScopeInvalidation = true;
            }
            else
            {
                IncrementRenderVersion();
                hasPendingRenderContentInvalidation = true;
            }
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

        if (options.HasFlag(UiPropertyOptions.Inherits))
        {
            flags |= InvalidationFlags.Inherited;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsSemantics))
        {
            flags |= InvalidationFlags.Semantics;
        }

        return flags;
    }

    private static bool IsValidNormalizedPoint(LayoutPoint point)
    {
        return float.IsFinite(point.X) &&
            float.IsFinite(point.Y) &&
            point.X >= 0 &&
            point.X <= 1 &&
            point.Y >= 0 &&
            point.Y <= 1;
    }

    private static bool IsValidLayoutMotionId(LayoutMotionId? id)
    {
        return id is null || !string.IsNullOrWhiteSpace(id.Value.Value);
    }

    private static bool IsRenderScopeProperty(UiProperty property)
    {
        return ReferenceEquals(property, RenderTransformProperty) ||
            ReferenceEquals(property, RenderTransformOriginProperty) ||
            ReferenceEquals(property, OpacityProperty) ||
            ReferenceEquals(property, TranslateXProperty) ||
            ReferenceEquals(property, TranslateYProperty) ||
            ReferenceEquals(property, ScaleProperty) ||
            ReferenceEquals(property, ScaleXProperty) ||
            ReferenceEquals(property, ScaleYProperty) ||
            ReferenceEquals(property, RotationProperty) ||
            ReferenceEquals(property, SkewXProperty) ||
            ReferenceEquals(property, SkewYProperty) ||
            ReferenceEquals(property, ClipToBoundsProperty);
    }
}

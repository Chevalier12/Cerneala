using System.Runtime.ExceptionServices;
using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Elements;

public partial class UIElement : UiObject, IUiPropertyOwner, ILayoutElement, IRenderableElement
{
    private static long nextPrismVisualNodeId;
    private readonly ElementAspectConsumer aspectConsumer;
    private IDisposable? elementAspectBehavior;
    // The aspect whose consumers include this element: its SetValue invalidates the element.
    private ElementAspect? followedAspect;
    private readonly long prismVisualNodeId;
    private ImageResourceLeaseSet? sourceImageLeases;
    private ImageResourceLeaseSet? prismImageLeases;

    internal ImageResourceLeaseSet SourceImageLeases => sourceImageLeases ??= new();

    internal ImageResourceLeaseSet PrismImageLeases => prismImageLeases ??= new();

    internal bool HoldsImageResources =>
        sourceImageLeases?.Count > 0 || prismImageLeases?.Count > 0;

    // Advances whenever this element releases the images it holds.
    internal int ImageReleaseGeneration { get; private set; }

    internal void ReleaseImageResources()
    {
        ImageReleaseGeneration++;
        try { sourceImageLeases?.Clear(); }
        finally { prismImageLeases?.Clear(); }
    }

    public static readonly UiProperty<object?> DataContextProperty = UiProperty<object?>.Register(
        nameof(DataContext),
        typeof(UIElement),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.Inherits | UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<ElementAspect?> AspectProperty = UiProperty<ElementAspect?>.Register(
        nameof(Aspect),
        typeof(UIElement),
        new UiPropertyMetadata<ElementAspect?>(null, UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<bool> IsEnabledProperty = UiProperty<bool>.Register(
        nameof(IsEnabled),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsVisibleProperty = UiProperty<bool>.Register(
        nameof(IsVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsHitTestVisibleProperty = UiProperty<bool>.Register(
        nameof(IsHitTestVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<Thickness> MarginProperty = UiProperty<Thickness>.Register(
        nameof(Margin),
        typeof(UIElement),
        new UiPropertyMetadata<Thickness>(Thickness.Zero, UiPropertyOptions.AffectsMeasure));

    public static readonly UiProperty<float> WidthProperty = UiProperty<float>.Register(
        nameof(Width),
        typeof(UIElement),
        new UiPropertyMetadata<float>(float.NaN, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidDimension));

    public static readonly UiProperty<float> HeightProperty = UiProperty<float>.Register(
        nameof(Height),
        typeof(UIElement),
        new UiPropertyMetadata<float>(float.NaN, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidDimension));

    public static readonly UiProperty<float> MinWidthProperty = UiProperty<float>.Register(
        nameof(MinWidth),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidMinimumDimension));

    public static readonly UiProperty<float> MaxWidthProperty = UiProperty<float>.Register(
        nameof(MaxWidth),
        typeof(UIElement),
        new UiPropertyMetadata<float>(float.PositiveInfinity, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidMaximumDimension));

    public static readonly UiProperty<float> MinHeightProperty = UiProperty<float>.Register(
        nameof(MinHeight),
        typeof(UIElement),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidMinimumDimension));

    public static readonly UiProperty<float> MaxHeightProperty = UiProperty<float>.Register(
        nameof(MaxHeight),
        typeof(UIElement),
        new UiPropertyMetadata<float>(float.PositiveInfinity, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange, validateValue: IsValidMaximumDimension));

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
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsKeyboardFocusedProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocused),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsKeyboardFocusWithinProperty = UiProperty<bool>.Register(
        nameof(IsKeyboardFocusWithin),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> FocusableProperty = UiProperty<bool>.Register(
        nameof(Focusable),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<bool> IsTabStopProperty = UiProperty<bool>.Register(
        nameof(IsTabStop),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsAspect));

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
        aspectConsumer = new ElementAspectConsumer(this);
        prismVisualNodeId = NextPrismVisualNodeId();
        LogicalChildren = new UIElementCollection(this, ElementChildRole.Logical);
        VisualChildren = new UIElementCollection(this, ElementChildRole.Visual);
        Handlers = new ElementHandlerStore(this);
        CommandBindings = new CommandBindingCollection(this);
        Bindings = new BindingSubscriptionCollection();
        Resources = new ResourceDictionary();
        Resources.ResourceChanged += OnElementResourceChanged;
    }

    public UIElement? LogicalParent { get; private set; }

    public UIElement? VisualParent { get; private set; }

    public UIElementCollection LogicalChildren { get; }

    public UIElementCollection VisualChildren { get; }

    public UIRoot? Root { get; private set; }

    public bool IsAttached => Root is not null;

    public bool IsLoaded => IsAttached;

    // True from the start of DetachFromRoot until Root is cleared.
    internal bool IsDetachingFromRoot => isDetachingFromRoot;

    public bool IsInitialized => isInitialized;

    public ElementHandlerStore Handlers { get; }

    public CommandBindingCollection CommandBindings { get; }

    public BindingSubscriptionCollection Bindings { get; }

    public ResourceDictionary Resources { get; }

    public InputBindingCollection InputBindings { get; } = new();

    public DirtyState DirtyState { get; } = new();

    public LayoutSize DesiredSize { get; private set; }

    public LayoutRect ArrangedBounds { get; private set; }

    // Layout motion removes scrolling before reproducing the original rounding.
    internal LayoutPoint UnroundedArrangedLocation { get; private set; }

    internal LayoutRounding ArrangeRounding { get; private set; }

    public int LayoutVersion { get; private set; }

    public int RenderVersion { get; private set; }

    public int RenderScopeVersion { get; private set; }

    public RenderDependency RenderDependencies { get; private set; } = RenderDependency.None;

    public bool IsLayoutBoundary { get; set; }

    internal long PrismVisualNodeId => prismVisualNodeId;

    internal long PrismLocalVisualVersion { get; private set; } = 1;

    internal long PrismVisualVersion { get; private set; } = 1;

    internal virtual UIElement? PrismVisualParent => VisualParent ?? retainedVisualParent;

    internal Transform LayoutCorrectionTransform { get; private set; } = Transform.Identity;

    // True while the presence coordinator plays this element's exit, whatever its input option: the element is still
    // rendered, so its queued work must stay queued (#940).
    internal bool IsPresenceExiting { get; private set; }

    // True while the element is exiting and its PresenceOptions.ExcludeInputWhileExiting removes it from input.
    internal bool IsPresenceInputExcluded { get; private set; }

    // A virtualizing panel keeps a child outside its realization window in VisualChildren but does not realize it,
    // so the child is not rendered, hit tested or routed input until the panel realizes it again.
    internal bool IsSkippedByVirtualization { get; private set; }

    internal virtual bool ActivatesOnPointerRelease => true;

    // True for an element that is not a command source but owns the pointer presses on it as its own click, as a WPF
    // ButtonBase in its template handles MouseLeftButtonDown, such as the field of a non-editable ComboBox, or as a
    // WPF ListBoxItem, ComboBoxItem or TabItem handles MouseLeftButtonDown to select itself. The input bridge resolves
    // such a press to this element, so an enclosing command source is not pressed, does not repeat, and does not click
    // for it (#589).
    internal virtual bool OwnsPointerClick => IsSelectorItemContainer && IsEnabled;

    // True while a Selector hosts this element as an item container that a click selects.
    internal bool IsSelectorItemContainer { get; set; }

    internal virtual TimeSpan? PointerRepeatDelay => null;

    internal virtual TimeSpan PointerRepeatInterval => TimeSpan.Zero;

    public float PresenceOpacity { get; private set; } = 1;

    public float PresenceScale { get; private set; } = 1;

    protected override UiPropertyMutationObserver? MutationObserver => Root?.PropertyMutations;

    internal virtual Cerneala.UI.Relay.UiRelay? OwnerRelay => Root?.Relay;

    internal void VerifyOwnerAccess() => VerifyMutationAccess();

    protected override void VerifyMutationAccess() => OwnerRelay?.VerifyAccess();

    // True while MinWidth, MaxWidth, MinHeight or MaxHeight differs from its default (OnPropertyInvalidated).
    private bool hasSizeLimits;
    private bool hasPendingCommandStateRefresh;
    private bool hasPendingRenderScopeInvalidation;
    private bool hasPendingRenderContentInvalidation;
    private bool isPrismVisualBoundary;
    private UIElement? retainedVisualParent;
    private readonly List<IElementLifecycleBehavior> lifecycleBehaviors = [];
    private bool isInitialized;
    private int attachmentGeneration;
    private bool isDetachingFromRoot;

    internal LayoutSize? LastMeasureAvailableSize { get; set; }

    internal int LastMeasureLayoutVersion { get; set; } = -1;

    private LayoutSize? previousMeasureAvailableSize;
    private LayoutSize previousMeasureDesiredSize;
    private int previousMeasureLayoutVersion = -1;
    private LayoutSize? olderMeasureAvailableSize;
    private LayoutSize olderMeasureDesiredSize;
    private int olderMeasureLayoutVersion = -1;
    private LayoutSize? oldestMeasureAvailableSize;
    private LayoutSize oldestMeasureDesiredSize;
    private int oldestMeasureLayoutVersion = -1;
    private int measureCacheInvalidationCount;

    internal LayoutRect? LastArrangeFinalRect { get; set; }

    internal int LastArrangeLayoutVersion { get; set; } = -1;

    private int arrangeCacheInvalidationCount;

    // An arrange reads desired sizes: its own, by which the element aligns itself in its slot, and its visual
    // children's, by which it places them. Its cache stays valid while these equal the values from when the arrange
    // started, so a measure that changes one and a later measure that gives the old value back keep the cache.
    private LayoutSize arrangeDesiredSize;
    private int arrangeGeneration;
    private int changedChildDesiredSizeCount;
    private int parentArrangeGeneration = -1;
    private LayoutSize parentArrangeDesiredSize;

    public object? DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    public ElementAspect? Aspect
    {
        get => GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

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

    public bool IsHitTestVisible
    {
        get => GetValue(IsHitTestVisibleProperty);
        set => SetValue(IsHitTestVisibleProperty, value);
    }

    public Thickness Margin
    {
        get => GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MinHeight
    {
        get => GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
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

    public bool IsMouseOver
    {
        get => IsPointerOver;
    }

    public bool IsMouseDirectlyOver
    {
        get => IsPointerOver;
        set => IsPointerOver = value;
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

    public bool TryFindResource(object key, out object? resource)
    {
        ArgumentNullException.ThrowIfNull(key);
        for (UIElement? current = this; current is not null; current = current.LogicalParent ?? current.VisualParent)
        {
            if (current.Resources.TryGetValue(key, out resource))
            {
                return true;
            }
        }

        resource = null;
        return false;
    }

    public object? FindResource(object key)
    {
        return TryFindResource(key, out object? resource)
            ? resource
            : throw new KeyNotFoundException($"Resource '{key}' was not found in the element tree.");
    }

    public bool TryFindResource<T>(object key, out T resource)
    {
        ArgumentNullException.ThrowIfNull(key);
        for (UIElement? current = this; current is not null; current = current.LogicalParent ?? current.VisualParent)
        {
            if (current.Resources.TryGetResource(key, out resource))
            {
                return true;
            }

            if (current.Resources.ContainsKey(key))
            {
                resource = default!;
                return false;
            }
        }

        if (key is string text && Root?.ResourceProvider is IResourceProvider provider)
        {
            ResourceId<T> id = new(text);
            if (provider.TryGetResource(id, out resource))
            {
                Root.ResourceDependencyTracker.RecordDependency(this, id);
                return true;
            }
        }

        resource = default!;
        return false;
    }

    public bool TryFindResource<T>(ResourceId<T> id, out T resource)
    {
        return TryFindResource(id.Key, out resource);
    }

    public T FindResource<T>(object key)
    {
        return TryFindResource(key, out T resource)
            ? resource
            : throw new KeyNotFoundException($"Resource '{key}' with type '{typeof(T).FullName}' was not found in the element tree or root provider.");
    }

    public T FindResource<T>(ResourceId<T> id)
    {
        return FindResource<T>(id.Key);
    }

    // A parent attaches this element only through its LogicalChildren or VisualChildren. A TabItem gives the Content
    // it holds this LogicalParent without adding it to its LogicalChildren, so that LogicalParent does not attach it.
    internal bool HasAttachedParent =>
        (LogicalParent is { Root: not null } logicalParent && logicalParent.LogicalChildren.IndexOf(this) >= 0) ||
        (VisualParent?.Root is not null);

    private void OnElementResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        Root?.AdvanceImageResolutionEpoch();
        InvalidationFlags flags = InvalidationFlags.Resource | InvalidationFlags.Aspect | InvalidationFlags.Subtree;

        UIRoot? root = Root;
        if (root is null)
        {
            IncrementPrismVisualVersion();
            Invalidate(flags, "Element resources changed");
            return;
        }

        if (root.Relay.CheckAccess())
        {
            IncrementPrismVisualVersion();
            Invalidate(flags, "Element resources changed");
            return;
        }

        WeakReference<UIElement> element = new(this);
        int generation = Volatile.Read(ref attachmentGeneration);
        root.Relay.Post(() =>
        {
            if (element.TryGetTarget(out UIElement? target) &&
                ReferenceEquals(target.Root, root) &&
                Volatile.Read(ref target.attachmentGeneration) == generation)
            {
                target.IncrementPrismVisualVersion();
                target.Invalidate(flags, "Element resources changed");
            }
        });
    }

    internal void SetLogicalParent(UIElement? parent)
    {
        LogicalParent = parent;
        // Resource lookup walks logical, then visual, ancestry.
        Root?.AdvanceImageResolutionEpoch();
    }

    internal virtual void ValidateParentChange(UIElement? parent, ElementChildRole role, bool ownerManaged)
    {
    }

    internal virtual void ValidateChildInsertion(UIElement child, ElementChildRole role, bool ownerManaged)
    {
    }

    // A parent can let a new owner take one of its children without removing it first. ContentPresenter does so for
    // the element content it hosts directly while its template runs, so the element leaves the presenter only when
    // the template adds it to another element.
    internal virtual void ReleaseChildForNewOwner(UIElement child)
    {
    }

    internal void ReleaseFromParentForNewOwner()
    {
        VisualParent?.ReleaseChildForNewOwner(this);
        LogicalParent?.ReleaseChildForNewOwner(this);
    }

    internal void SetVisualParent(UIElement? parent)
    {
        if (!ReferenceEquals(VisualParent, parent))
        {
            // A changed desired size counts against the arrange of the parent that read the old value, not a new parent.
            if (VisualParent is UIElement previousParent &&
                parentArrangeGeneration == previousParent.arrangeGeneration &&
                DesiredSize != parentArrangeDesiredSize)
            {
                previousParent.changedChildDesiredSizeCount--;
            }

            parentArrangeGeneration = -1;
        }

        VisualParent = parent;
        // Being skipped belongs to the panel that skipped the child, not to a new parent.
        IsSkippedByVirtualization = false;
        Root?.AdvanceImageResolutionEpoch();
        if (parent is not null)
        {
            retainedVisualParent = null;
        }
    }

    internal void SetRetainedVisualParent(UIElement? parent)
    {
        retainedVisualParent = parent;
    }

    internal void SetPrismVisualBoundary(bool isBoundary)
    {
        isPrismVisualBoundary = isBoundary;
    }

    internal void InvalidatePrismAttachment()
    {
        prismImageLeases?.Clear();
        InvalidateRenderScope("Prism attachment changed");
    }

    internal void AttachToRoot(UIRoot root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        root.AdvanceImageResolutionEpoch();
        attachmentGeneration++;

        // Behaviors the Aspect behavior adds attach themselves (the element is
        // attached by then); only the ones present before are attached here.
        IElementLifecycleBehavior[] existingBehaviors = [.. lifecycleBehaviors];
        AttachElementAspectBehavior();
        root.AspectProcessor.AttachBehaviors(this);
        if (!isInitialized)
        {
            isInitialized = true;
            Initialized?.Invoke(this, EventArgs.Empty);
        }
        OnAttached();
        foreach (IElementLifecycleBehavior behavior in existingBehaviors)
        {
            behavior.Attach();
        }
        Root?.Motion.Layout.MarkAttached(this);
        Root?.Motion.Presence.MarkAttached(this);
        RaiseEvent(new RoutedEventArgs(LoadedEvent, this));
    }

    internal virtual void ValidateLifecycleRoot(UIRoot root)
    {
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            behavior.ValidateRoot(root);
        }
    }

    internal void DetachFromRoot()
    {
        attachmentGeneration++;
        isDetachingFromRoot = true;
        // A failing detach callback does not skip the element's later callbacks: each one runs, the element is
        // detached, then the first failure propagates.
        ExceptionDispatchInfo? failure = null;
        try
        {
            try { DetachElementAspectBehavior(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { RaiseEvent(new RoutedEventArgs(UnloadedEvent, this)); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { Root?.Motion.Layout.MarkDetached(this); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { Root?.Motion.Presence.MarkDetached(this); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
            {
                try { behavior.Detach(); }
                catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            }
            try { OnDetached(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
        }
        finally
        {
            // A failing detach callback or cleanup step still leaves the element detached. A cleanup step does not
            // throw from this finally block, because that would skip the remaining steps and replace the earlier
            // failure.
            try { ReleaseImageResources(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { Root?.RetainedRenderCache.ReleaseElement(this); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { Bindings.Clear(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            try { Root?.AdvanceImageResolutionEpoch(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
            Root = null;
            isDetachingFromRoot = false;
        }

        failure?.Throw();
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

    internal void AddLifecycleBehavior(IElementLifecycleBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        lifecycleBehaviors.Add(behavior);
    }

    internal void RemoveLifecycleBehavior(IElementLifecycleBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        lifecycleBehaviors.Remove(behavior);
    }

    // Diagnostic view of the attached lifecycle behaviors.
    internal IReadOnlyList<IElementLifecycleBehavior> LifecycleBehaviors => lifecycleBehaviors;

    private void NotifyLifecycleRenderabilityChanged()
    {
        bool isRenderable = UIElementVisibility.IsEffectivelyVisible(this);
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            behavior.OnRenderabilityChanged(isRenderable);
        }
    }

    internal void ValidateDataLifecycle(Cerneala.UI.Relay.UiRelay relay)
    {
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            if (behavior is IElementDataLifecycleBehavior data) { data.ValidateRelay(relay); }
        }
    }

    internal void AttachDataLifecycle()
    {
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors.ToArray())
        {
            if (behavior is IElementDataLifecycleBehavior) { behavior.Attach(); }
        }
    }

    internal void DetachDataLifecycle()
    {
        List<Exception>? failures = null;
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors.ToArray())
        {
            if (behavior is not IElementDataLifecycleBehavior) { continue; }
            try { behavior.Detach(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        try { Bindings.Clear(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        if (failures is not null) { throw new AggregateException(failures); }
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
        if (TryUseCachedMeasure(context.AvailableSize, out LayoutSize cached))
        {
            RecordScheduledMeasureVisit();
            return cached;
        }

        Root?.CountMeasureCall();
        // ArrangeCore can read measure state other than the desired sizes, such as the heights of a Grid's * rows, which
        // keep their content height when measured unbounded. A measure that runs therefore makes the element's own
        // arrange cache invalid, as WPF's Measure calls InvalidateArrange, also when the desired size is unchanged (#516).
        // The parent's arrange cache still depends only on this element's desired size.
        LastArrangeLayoutVersion = -1;
        int invalidationCountBeforeMeasure = measureCacheInvalidationCount;
        LayoutSize desired = !UIElementVisibility.ParticipatesInLayout(this)
            ? LayoutSize.Zero
            : MeasureWithMargin(context);
        desired = context.Rounding.Round(desired);
        PreserveCurrentMeasureCache();
        SetDesiredSize(desired);
        LastMeasureAvailableSize = context.AvailableSize;
        // A Measure request raised during this measure (MeasureCore) stays pending: neither the
        // cache nor the scheduled visit may let a same-constraint measure or the queue entry skip it.
        if (measureCacheInvalidationCount != invalidationCountBeforeMeasure)
        {
            LastMeasureLayoutVersion = -1;
            return desired;
        }

        LastMeasureLayoutVersion = LayoutVersion;
        RecordScheduledMeasureVisit();
        return desired;
    }

    internal bool TryUseCachedMeasure(LayoutSize availableSize, out LayoutSize desiredSize)
    {
        if (LastMeasureAvailableSize == availableSize &&
            LastMeasureLayoutVersion == LayoutVersion)
        {
            desiredSize = DesiredSize;
            return true;
        }


        if (VisualChildren.Count > 0)
        {
            desiredSize = default;
            return false;
        }

        if (TryUsePreviousMeasureCache(
            availableSize,
            ref previousMeasureAvailableSize,
            ref previousMeasureDesiredSize,
            ref previousMeasureLayoutVersion,
            out desiredSize) ||
            TryUsePreviousMeasureCache(
                availableSize,
                ref olderMeasureAvailableSize,
                ref olderMeasureDesiredSize,
                ref olderMeasureLayoutVersion,
                out desiredSize) ||
            TryUsePreviousMeasureCache(
                availableSize,
                ref oldestMeasureAvailableSize,
                ref oldestMeasureDesiredSize,
                ref oldestMeasureLayoutVersion,
                out desiredSize))
        {
            return true;
        }

        desiredSize = default;
        return false;
    }

    internal void InvalidateMeasureCache()
    {
        LastMeasureAvailableSize = null;
        LastMeasureLayoutVersion = -1;
        previousMeasureAvailableSize = null;
        previousMeasureLayoutVersion = -1;
        olderMeasureAvailableSize = null;
        olderMeasureLayoutVersion = -1;
        oldestMeasureAvailableSize = null;
        oldestMeasureLayoutVersion = -1;
    }

    // Keeps LastMeasureAvailableSize: LayoutManager.GetAvailableSize reuses it as the element's
    // constraint, and ItemsPresenter reads it as the panel's last constraint.
    // Forgets the scheduled visit: a measure that ran before this request does not satisfy it.
    internal void InvalidateMeasureCacheKeepingConstraint()
    {
        LastMeasureLayoutVersion = -1;
        previousMeasureLayoutVersion = -1;
        olderMeasureLayoutVersion = -1;
        oldestMeasureLayoutVersion = -1;
        measureCacheInvalidationCount++;
        LastScheduledMeasureRoot = null;
    }

    // Keeps LastArrangeFinalRect: LayoutManager.GetFinalRect reuses it as the element's slot.
    // Forgets the scheduled visit: an arrange that ran before this request does not satisfy it.
    internal void InvalidateArrangeCache()
    {
        LastArrangeLayoutVersion = -1;
        arrangeCacheInvalidationCount++;
        LastScheduledArrangeRoot = null;
    }

    internal bool ArrangeDesiredSizesAreCurrent =>
        DesiredSize == arrangeDesiredSize && changedChildDesiredSizeCount == 0;

    internal bool HasValidArrangeCache(LayoutRect finalRect)
    {
        return LastArrangeFinalRect == finalRect &&
            LastArrangeLayoutVersion == LayoutVersion &&
            ArrangeDesiredSizesAreCurrent;
    }

    public LayoutRect Arrange(ArrangeContext context)
    {
        if (HasValidArrangeCache(context.FinalRect))
        {
            RecordScheduledArrangeVisit();
            return ArrangedBounds;
        }

        Root?.CountArrangeCall();
        int invalidationCountBeforeArrange = arrangeCacheInvalidationCount;
        // The desired sizes this arrange reads, the element's own and its visual children's, are the current ones.
        // An arrange that does not finish leaves no valid cache with them.
        LastArrangeLayoutVersion = -1;
        arrangeDesiredSize = DesiredSize;
        arrangeGeneration++;
        changedChildDesiredSizeCount = 0;
        LayoutRect finalRect = !UIElementVisibility.ParticipatesInLayout(this)
            ? context.FinalRect
            : ApplyMarginAndAlignment(context.FinalRect, context.Rounding);

        // ArrangeCore receives the element's own rounded bounds, as WPF rounds an element's offset and arrange size
        // and arranges its children inside them. From an unrounded origin such as 0.5, each child would round on its
        // own (ties to even) and children that should touch would leave gaps or overlap; with the rounded origin but
        // the unrounded size, a child aligned to the far edge would end past the element's rounded far edge.
        LayoutRect arranged = !UIElementVisibility.ParticipatesInLayout(this)
            ? new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0)
            : ArrangeCore(new ArrangeContext(context.Rounding.Round(finalRect), context.Rounding));
        UnroundedArrangedLocation = finalRect.Location;
        ArrangeRounding = context.Rounding;
        arranged = context.Rounding.Round(arranged);
        SetArrangedBounds(arranged);
        LastArrangeFinalRect = context.FinalRect;
        // An Arrange request raised during this arrange (ArrangeCore, SizeChanged) stays pending:
        // neither the cache nor the scheduled visit may let a same-slot arrange or the queue entry skip it.
        if (arrangeCacheInvalidationCount != invalidationCountBeforeArrange)
        {
            LastArrangeLayoutVersion = -1;
            return arranged;
        }

        LastArrangeLayoutVersion = LayoutVersion;
        RecordScheduledArrangeVisit();
        return arranged;
    }

    internal long LastScheduledMeasurePass { get; private set; }

    internal UIRoot? LastScheduledMeasureRoot { get; private set; }

    internal int LastScheduledMeasureLayoutVersion { get; private set; } = -1;

    internal long LastScheduledArrangePass { get; private set; }

    internal UIRoot? LastScheduledArrangeRoot { get; private set; }

    internal int LastScheduledArrangeLayoutVersion { get; private set; } = -1;

    private void RecordScheduledMeasureVisit()
    {
        if (Root?.Scheduler.IsProcessingMeasure == true)
        {
            LastScheduledMeasurePass = Root.Scheduler.CurrentMeasurePass;
            LastScheduledMeasureRoot = Root;
            LastScheduledMeasureLayoutVersion = LayoutVersion;
        }
    }

    private void RecordScheduledArrangeVisit()
    {
        if (Root?.Scheduler.IsProcessingArrange == true)
        {
            LastScheduledArrangePass = Root.Scheduler.CurrentArrangePass;
            LastScheduledArrangeRoot = Root;
            LastScheduledArrangeLayoutVersion = LayoutVersion;
        }
    }

    protected virtual LayoutSize MeasureCore(MeasureContext context)
    {
        return LayoutSize.Zero;
    }

    // True when this element's measure leaves the visual child unmeasured on purpose, as a virtualizing panel
    // does outside its realization window. The frame then does not measure that child or its subtree on its own.
    internal virtual bool SkipsChildMeasure(UIElement child)
    {
        return false;
    }

    protected virtual LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }

    // The visual child at index in drawing order, bottom to top; a Panel orders its children by Panel.ZIndex.
    internal virtual UIElement GetVisualChildInZOrder(int index)
    {
        return VisualChildren[index];
    }

    // Called when a visual child's Panel.ZIndex changes. VisualChildren mutations are detected through
    // UIElementCollection.Version instead.
    internal virtual void InvalidateVisualChildZOrder()
    {
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
        if (DesiredSize == desiredSize)
        {
            return;
        }

        WriteDesiredSize(desiredSize);
        RequireArrangeForDesiredSizeChange();
        if (VisualParent is UIElement parent)
        {
            parent.RequireArrangeForDesiredSizeChange();
        }
    }

    // A stale arrange cache (ArrangeDesiredSizesAreCurrent) takes effect only when an ancestor arranges this element
    // again. An ancestor whose slot and desired size did not change returns its cached result without arranging its
    // children (a Border between a Canvas and a StackPanel whose children swapped heights, #450), so the frame arranges
    // this element itself with its last slot, as WPF's InvalidateArrange puts the element in the arrange queue. The
    // queued arrange still reuses the cache when the desired sizes it read have come back (HasValidArrangeCache).
    private void RequireArrangeForDesiredSizeChange()
    {
        Root?.LayoutQueue.RequireArrange(this);
    }

    // A parent that skips measuring this element (VirtualizingStackPanel gives an unrealized row LayoutSize.Zero) writes
    // a desired size that no measure produced, so the current cache entry stops matching: the next Measure with the same
    // constraint runs MeasureCore instead of returning that size. The constraint and the older entries, which hold real
    // measure results, stay.
    internal void SetUnmeasuredDesiredSize(LayoutSize desiredSize)
    {
        WriteDesiredSize(desiredSize);
        LastMeasureLayoutVersion = -1;
    }

    // An arrange reads the desired sizes: the element aligns itself in its slot by its own, and its parent places it by
    // it (a Canvas or StackPanel child gets its desired height). While one differs from the value it had when the
    // arrange started, a same-slot arrange of the element or of its parent runs again (ArrangeDesiredSizesAreCurrent).
    // This covers a measure from inside the parent's MeasureCore, which the frame step that resets the parent's arrange
    // cache does not see, for example in a row that a VirtualizingStackPanel realizes again after skipping it. A measure
    // that gives the old value back keeps both caches, as when an overlay layer measures its content unbounded and then
    // clamped to the space beside the target.
    private void WriteDesiredSize(LayoutSize desiredSize)
    {
        LayoutSize previousDesiredSize = DesiredSize;
        DesiredSize = desiredSize;
        if (VisualParent is not UIElement parent)
        {
            return;
        }

        if (parentArrangeGeneration != parent.arrangeGeneration)
        {
            // The first change since the parent's arrange started: that arrange read the previous value.
            parentArrangeGeneration = parent.arrangeGeneration;
            parentArrangeDesiredSize = previousDesiredSize;
        }

        bool wasChanged = previousDesiredSize != parentArrangeDesiredSize;
        bool isChanged = desiredSize != parentArrangeDesiredSize;
        if (wasChanged != isChanged)
        {
            parent.changedChildDesiredSizeCount += isChanged ? 1 : -1;
        }
    }

    internal bool SetArrangedBounds(LayoutRect arrangedBounds)
    {
        if (ArrangedBounds == arrangedBounds)
        {
            return false;
        }

        LayoutRect previousBounds = ArrangedBounds;
        ArrangedBounds = arrangedBounds;
        IncrementRenderVersion();
        Root?.RetainedRenderCache.InvalidateRoot();
        Root?.RenderQueue.Enqueue(this);
        Root?.OverlayManager.OnElementArranged(this);
        RaiseEvent(new SizeChangedEventArgs(SizeChangedEvent, this, previousBounds.Size, arrangedBounds.Size));
        return true;
    }

    internal void IncrementLayoutVersion()
    {
        LayoutVersion++;
    }

    internal void IncrementRenderVersion()
    {
        RenderVersion++;
        IncrementPrismVisualVersion();
    }

    internal void IncrementPrismDescendantVisualVersion()
    {
        IncrementPrismAggregateVersion();
    }

    internal bool ConsumeRenderScopeOnlyInvalidation()
    {
        bool isScopeOnly = hasPendingRenderScopeInvalidation && !hasPendingRenderContentInvalidation;
        hasPendingRenderScopeInvalidation = false;
        hasPendingRenderContentInvalidation = false;
        return isScopeOnly;
    }

    internal void MarkRenderContentInvalidation()
    {
        hasPendingRenderContentInvalidation = true;
    }

    private void InvalidateRenderScope(string reason)
    {
        hasPendingRenderScopeInvalidation = true;
        Invalidate(new InvalidationRequest(this, InvalidationFlags.Render, reason) { IsRenderScopeOnly = true });
    }

    internal void SetLayoutCorrectionTransform(Transform correction)
    {
        ArgumentNullException.ThrowIfNull(correction);
        if (LayoutCorrectionTransform == correction)
        {
            return;
        }

        LayoutCorrectionTransform = correction;
        IncrementRenderScopeVersion();
        InvalidateRenderScope("Layout motion correction changed");
    }

    internal void SetPresenceExiting(bool isExiting, bool excludesInput)
    {
        bool isInputExcluded = isExiting && excludesInput;
        if (IsPresenceExiting == isExiting && IsPresenceInputExcluded == isInputExcluded)
        {
            return;
        }

        IsPresenceExiting = isExiting;
        IsPresenceInputExcluded = isInputExcluded;
        Invalidate(InvalidationFlags.HitTest, "Presence state changed");
    }

    // An exiting element's descendants left VisualChildren with it and are still rendered through it.
    internal bool IsInPresenceExitingSubtree()
    {
        for (UIElement? current = this; current is not null; current = current.VisualParent)
        {
            if (current.IsPresenceExiting)
            {
                return true;
            }
        }

        return false;
    }

    internal void SetSkippedByVirtualization(bool isSkipped)
    {
        if (IsSkippedByVirtualization == isSkipped)
        {
            return;
        }

        // Rendering, hit testing and input stop or resume as when IsVisible changes.
        IsSkippedByVirtualization = isSkipped;
        IncrementRenderVersion();
        hasPendingRenderContentInvalidation = true;
        if (isSkipped && Root is UIRoot motionRoot)
        {
            motionRoot.Motion.CancelMotionForSubtree(this);
        }

        if (IsAttached)
        {
            foreach (UIElement element in ElementTreeWalker.PreOrderRenderability(this))
            {
                element.NotifyLifecycleRenderabilityChanged();
            }
        }

        Invalidate(InvalidationFlags.Render | InvalidationFlags.HitTest | InvalidationFlags.Semantics, "Virtualization realization changed");
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
        IncrementRenderScopeVersion();
        InvalidateRenderScope("Presence visual state changed");
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
        // An explicit dimension is rounded to the nearest device pixel before it constrains measure and becomes the
        // desired size, as WPF rounds an explicit Width and Height. ApplyAlignment arranges at the same rounded value,
        // so 99.4 is measured and arranged at 99 instead of measured at 99 and arranged at the rounded-up 100.
        SizeLimits horizontal = GetHorizontalSizeLimits(context.Rounding);
        SizeLimits vertical = GetVerticalSizeLimits(context.Rounding);
        LayoutSize constrainedSize = new(
            horizontal.ConstrainAvailable(contentAvailableSize.Width),
            vertical.ConstrainAvailable(contentAvailableSize.Height));
        // MeasureCore receives the size rounded to the nearest device pixel, as WPF rounds the size it passes to
        // MeasureOverride. Arrange rounds the rectangle ArrangeCore receives the same way, so an element that fills
        // its slot is measured at the size it is then arranged at (99.6 would be measured but 100 arranged).
        LayoutSize roundedSize = new(
            context.Rounding.Round(constrainedSize.Width),
            context.Rounding.Round(constrainedSize.Height));
        LayoutSize measuredSize = MeasureCore(new MeasureContext(roundedSize, context.Rounding)).ClampNonNegative();
        LayoutSize contentDesiredSize = new(
            horizontal.ClampDesired(measuredSize.Width),
            vertical.ClampDesired(measuredSize.Height));
        // With layout rounding the margin adds device pixels to the content, as a panel adds its children: at scale 1.5
        // an explicit 2.5 is 4 device pixels (4 / 1.5f) and a margin of 2 is 3, but their float sum times 1.5 is slightly
        // above 7, which Measure would round up to an 8th device pixel.
        LayoutRounding rounding = context.Rounding;
        return new LayoutSize(
            rounding.FromDevicePixels(rounding.ToDevicePixels(contentDesiredSize.Width) + rounding.ToDevicePixels(margin.Horizontal)),
            rounding.FromDevicePixels(rounding.ToDevicePixels(contentDesiredSize.Height) + rounding.ToDevicePixels(margin.Vertical)))
            .ClampNonNegative();
    }

    private void PreserveCurrentMeasureCache()
    {
        if (LastMeasureAvailableSize is null || LastMeasureLayoutVersion != LayoutVersion)
        {
            return;
        }

        oldestMeasureAvailableSize = olderMeasureAvailableSize;
        oldestMeasureLayoutVersion = olderMeasureLayoutVersion;
        oldestMeasureDesiredSize = olderMeasureDesiredSize;
        olderMeasureAvailableSize = previousMeasureAvailableSize;
        olderMeasureLayoutVersion = previousMeasureLayoutVersion;
        olderMeasureDesiredSize = previousMeasureDesiredSize;
        previousMeasureAvailableSize = LastMeasureAvailableSize;
        previousMeasureLayoutVersion = LastMeasureLayoutVersion;
        previousMeasureDesiredSize = DesiredSize;
    }

    private bool TryUsePreviousMeasureCache(
        LayoutSize availableSize,
        ref LayoutSize? cachedAvailableSize,
        ref LayoutSize cachedDesiredSize,
        ref int cachedLayoutVersion,
        out LayoutSize desiredSize)
    {
        if (cachedAvailableSize != availableSize || cachedLayoutVersion != LayoutVersion)
        {
            desiredSize = default;
            return false;
        }

        LayoutSize? currentAvailableSize = LastMeasureAvailableSize;
        int currentLayoutVersion = LastMeasureLayoutVersion;
        LayoutSize currentDesiredSize = DesiredSize;

        LastMeasureAvailableSize = cachedAvailableSize;
        LastMeasureLayoutVersion = cachedLayoutVersion;
        SetDesiredSize(cachedDesiredSize);

        cachedAvailableSize = currentAvailableSize;
        cachedLayoutVersion = currentLayoutVersion;
        cachedDesiredSize = currentDesiredSize;
        desiredSize = DesiredSize;
        return true;
    }

    private LayoutRect ApplyMarginAndAlignment(LayoutRect finalRect, LayoutRounding rounding)
    {
        Thickness margin = Margin;
        LayoutRect contentRect = new(
            finalRect.X + margin.Left,
            finalRect.Y + margin.Top,
            MathF.Max(0, finalRect.Width - margin.Horizontal),
            MathF.Max(0, finalRect.Height - margin.Vertical));
        LayoutSize contentDesiredSize = Deflate(DesiredSize, margin);
        return ApplyAlignment(contentRect, contentDesiredSize, rounding);
    }

    private static LayoutSize Deflate(LayoutSize size, Thickness thickness)
    {
        return new LayoutSize(
            MathF.Max(0, size.Width - thickness.Horizontal),
            MathF.Max(0, size.Height - thickness.Vertical));
    }

    private LayoutRect ApplyAlignment(LayoutRect finalRect, LayoutSize desiredSize, LayoutRounding rounding)
    {
        // DesiredSize is rounded up, so it can exceed the explicit dimension that measure rounded to the nearest
        // device pixel (99.4 is measured at 99, but a 0.3 margin rounds the desired 99.3 up to 100). The arranged
        // size is capped at that rounded explicit dimension, as WPF caps the arrange size at the rounded Height.
        SizeLimits horizontal = GetHorizontalSizeLimits(rounding);
        SizeLimits vertical = GetVerticalSizeLimits(rounding);
        float width = horizontal.ClampArranged(!horizontal.HasExplicitDimension && HorizontalAlignment == HorizontalAlignment.Stretch
            ? finalRect.Width
            : Math.Min(desiredSize.Width, finalRect.Width));
        float height = vertical.ClampArranged(!vertical.HasExplicitDimension && VerticalAlignment == VerticalAlignment.Stretch
            ? finalRect.Height
            : Math.Min(desiredSize.Height, finalRect.Height));

        // As WPF aligns a stretched element that is narrower than its slot, a MaxWidth or an explicit Width centers it.
        // An element that MinWidth makes wider than its slot starts at the slot's left edge when stretched.
        float x = HorizontalAlignment switch
        {
            HorizontalAlignment.Center => finalRect.X + ((finalRect.Width - width) / 2),
            HorizontalAlignment.Right => finalRect.X + finalRect.Width - width,
            HorizontalAlignment.Stretch when width <= finalRect.Width => finalRect.X + ((finalRect.Width - width) / 2),
            _ => finalRect.X
        };

        float y = VerticalAlignment switch
        {
            VerticalAlignment.Center => finalRect.Y + ((finalRect.Height - height) / 2),
            VerticalAlignment.Bottom => finalRect.Y + finalRect.Height - height,
            VerticalAlignment.Stretch when height <= finalRect.Height => finalRect.Y + ((finalRect.Height - height) / 2),
            _ => finalRect.Y
        };

        return new LayoutRect(x, y, width, height);
    }

    // Layout reads the four limit properties only on an element that sets one of them: every measure and arrange reads
    // the limits, and reading the defaults would make each about 45 % slower.
    private SizeLimits GetHorizontalSizeLimits(LayoutRounding rounding) => hasSizeLimits
        ? SizeLimits.Create(Width, MinWidth, MaxWidth, rounding)
        : SizeLimits.Create(rounding.Round(Width));

    private SizeLimits GetVerticalSizeLimits(LayoutRounding rounding) => hasSizeLimits
        ? SizeLimits.Create(Height, MinHeight, MaxHeight, rounding)
        : SizeLimits.Create(rounding.Round(Height));

    // The size limits of one axis, as WPF's FrameworkElement.MinMax: MinWidth wins over a smaller Width and over a
    // smaller MaxWidth, and MaxWidth wins over a larger Width. Each value is rounded to the nearest device pixel, as an
    // explicit Width is. Width = 50 with MinWidth = 80 gives Lower = Upper = 80.
    private readonly struct SizeLimits
    {
        private SizeLimits(float minimum, float lower, float upper, bool hasExplicitDimension)
        {
            Minimum = minimum;
            Lower = lower;
            Upper = upper;
            HasExplicitDimension = hasExplicitDimension;
        }

        // MinWidth: the one limit that wins over a smaller slot. A larger explicit Width keeps the slot's size.
        public float Minimum { get; }

        // The least desired size: the explicit Width within the limits, or MinWidth.
        public float Lower { get; }

        // The largest size: the explicit Width or MaxWidth, whichever is smaller, but at least MinWidth.
        public float Upper { get; }

        public bool HasExplicitDimension { get; }

        public static SizeLimits Create(float dimension, float minimum, float maximum, LayoutRounding rounding)
        {
            float explicitDimension = rounding.Round(dimension);
            float roundedMinimum = rounding.Round(minimum);
            bool hasExplicitDimension = !float.IsNaN(explicitDimension);
            float upper = MathF.Max(MathF.Min(hasExplicitDimension ? explicitDimension : float.PositiveInfinity, rounding.Round(maximum)), roundedMinimum);
            float lower = MathF.Max(MathF.Min(upper, hasExplicitDimension ? explicitDimension : 0), roundedMinimum);
            return new SizeLimits(roundedMinimum, lower, upper, hasExplicitDimension);
        }

        // The limits with MinWidth and MaxWidth at their defaults: the explicit Width alone, or none.
        public static SizeLimits Create(float explicitDimension) => float.IsNaN(explicitDimension)
            ? new SizeLimits(0, 0, float.PositiveInfinity, false)
            : new SizeLimits(0, explicitDimension, explicitDimension, true);

        // MeasureCore receives at most the upper limit, and at least MinWidth even when the slot is smaller.
        public float ConstrainAvailable(float available) => Clamp(available, Minimum, Upper);

        public float ClampDesired(float measured) => Clamp(measured, Lower, Upper);

        public float ClampArranged(float arranged) => Clamp(arranged, Minimum, Upper);

        // Layout sizes are never NaN, so plain comparisons replace MathF.Min and MathF.Max, which also order NaN and
        // signed zeros and cost a measurable part of each measure and arrange.
        private static float Clamp(float value, float minimum, float maximum)
        {
            float limited = value < maximum ? value : maximum;
            return limited > minimum ? limited : minimum;
        }
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

        if (IsSizeLimitProperty(args.Property))
        {
            hasSizeLimits = MinWidth != 0 || MinHeight != 0 ||
                !float.IsPositiveInfinity(MaxWidth) || !float.IsPositiveInfinity(MaxHeight);
        }

        bool isRenderScopeChange = false;
        if ((options & (UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual)) != UiPropertyOptions.None)
        {
            if (IsRenderScopeProperty(args.Property))
            {
                isRenderScopeChange = true;
                IncrementRenderScopeVersion();
                hasPendingRenderScopeInvalidation = true;
            }
            else
            {
                IncrementRenderVersion();
                hasPendingRenderContentInvalidation = true;
            }
        }

        if (ReferenceEquals(args.Property, Cerneala.UI.Layout.Panels.Panel.ZIndexProperty))
        {
            VisualParent?.InvalidateVisualChildZOrder();
        }

        // An attached property that only the parent reads (Canvas.Left) re-arranges the parent, not this element.
        if ((options & UiPropertyOptions.AffectsParentArrange) != UiPropertyOptions.None &&
            VisualParent is UIElement arrangeParent)
        {
            arrangeParent.IncrementLayoutVersion();
            arrangeParent.Invalidate(InvalidationFlags.Arrange, "Child attached property changed");
        }

        InvalidationFlags flags = MapInvalidationOptions(options);
        bool isVisibilityChange = ReferenceEquals(args.Property, VisibilityProperty) &&
            args.OldValue is Visibility &&
            args.NewValue is Visibility;
        Visibility oldVisibility = isVisibilityChange ? (Visibility)args.OldValue! : Visibility.Visible;
        Visibility newVisibility = isVisibilityChange ? (Visibility)args.NewValue! : Visibility.Visible;
        bool isIsVisibleChange = ReferenceEquals(args.Property, IsVisibleProperty) &&
            args.OldValue is bool &&
            args.NewValue is bool;
        bool becameNonVisible =
            (isVisibilityChange &&
                oldVisibility == Visibility.Visible &&
                newVisibility != Visibility.Visible) ||
            (isIsVisibleChange &&
                (bool)args.OldValue! &&
                !(bool)args.NewValue!);
        if (becameNonVisible && Root is UIRoot motionRoot)
        {
            motionRoot.Motion.CancelMotionForSubtree(this);
        }

        if ((isVisibilityChange || isIsVisibleChange) && IsAttached)
        {
            foreach (UIElement element in ElementTreeWalker.PreOrderRenderability(this))
            {
                element.NotifyLifecycleRenderabilityChanged();
            }
        }

        bool layoutParticipationChanged = isVisibilityChange &&
            (oldVisibility == Visibility.Collapsed) != (newVisibility == Visibility.Collapsed);
        bool expandingFromCollapsed = layoutParticipationChanged &&
            oldVisibility == Visibility.Collapsed;
        bool requiresParentLayoutScheduling = layoutParticipationChanged &&
            (Root is not UIRoot root || !root.Scheduler.IsProcessingLayout);
        if (expandingFromCollapsed)
        {
            foreach (UIElement descendant in ElementTreeWalker.Descendants(this, ElementChildRole.Visual))
            {
                descendant.IncrementLayoutVersion();
            }
        }

        if (flags != InvalidationFlags.None)
        {
            Invalidate(new InvalidationRequest(this, flags, "Property changed", args.Property) { IsRenderScopeOnly = isRenderScopeChange });
            // A participation change affects the parent's layout even when the child
            // is skipped by the scheduler because it has just become collapsed.
            if (requiresParentLayoutScheduling &&
                Root is UIRoot layoutRoot &&
                VisualParent is UIElement parent)
            {
                parent.IncrementLayoutVersion();
                layoutRoot.LayoutQueue.RequireMeasure(parent);
                layoutRoot.LayoutQueue.RequireArrange(parent);
            }
        }
    }

    private void ValidateLocalAspect(ElementAspect aspect)
    {
        if (!aspect.TargetType.IsInstanceOfType(this))
        {
            throw new InvalidOperationException(
                $"Element aspect '{aspect.Name ?? "<local>"}' targets '{aspect.TargetType.FullName}' and cannot be applied to '{GetType().FullName}'.");
        }

        foreach (ElementAspectValue value in aspect.DefaultValues)
        {
            ValidateLocalAspectProperty(value.Property);
        }

        foreach (ElementAspectCondition condition in aspect.Conditions)
        {
            foreach (ElementAspectValue value in condition.Values)
            {
                ValidateLocalAspectProperty(value.Property);
            }
        }
    }

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        if (ReferenceEquals(property, AspectProperty) && value is ElementAspect aspect)
        {
            ValidateLocalAspect(aspect);
        }
    }

    // Makes the element follow the aspect it reports: disposes the lifetime of the aspect it followed, moves its consumer
    // to the current aspect, and creates the current aspect's lifetime while attached. A failing lifetime or factory does
    // not skip the later steps; the first failure is kept in failure. The old lifetime's Dispose may assign another
    // aspect; that nested change already makes the element follow the newer aspect, so this one stops there.
    private void FollowAspect(ref ExceptionDispatchInfo? failure)
    {
        ElementAspect? aspect = Aspect;
        if (ReferenceEquals(followedAspect, aspect))
        {
            return;
        }

        if (followedAspect is ElementAspect previous)
        {
            followedAspect = null;
            previous.Detach(aspectConsumer);
            try { DetachElementAspectBehavior(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
        }

        if (aspect is null || followedAspect is not null || !ReferenceEquals(Aspect, aspect))
        {
            return;
        }

        aspect.Attach(aspectConsumer);
        followedAspect = aspect;
        if (IsAttached)
        {
            // A throwing factory leaves the element without a lifetime but still following the aspect; the next
            // attachment creates the lifetime.
            try { AttachElementAspectBehavior(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
        }
    }

    private void AttachElementAspectBehavior()
    {
        // The detach callbacks still see the element attached, but the detach has already disposed the lifetime
        // and leaves the element detached: an aspect they assign gets its lifetime on the next attach.
        if (!isDetachingFromRoot && elementAspectBehavior is null && Aspect is ElementAspect aspect)
        {
            int generation = attachmentGeneration;
            IDisposable? behavior = aspect.AttachBehavior(this);

            // The factory may replace or clear the aspect, or detach the element. Those
            // nested changes find no lifetime to dispose and may store a newer one; this
            // lifetime then belongs to an aspect or an attachment that is already gone.
            if (elementAspectBehavior is not null ||
                !ReferenceEquals(Aspect, aspect) ||
                attachmentGeneration != generation)
            {
                behavior?.Dispose();
                return;
            }

            elementAspectBehavior = behavior;
        }
    }

    private void DetachElementAspectBehavior()
    {
        // The lifetime is finished once its disposal starts, also when Dispose throws: the next
        // attach creates a new one, and the failed one is never disposed a second time.
        IDisposable? behavior = elementAspectBehavior;
        elementAspectBehavior = null;
        behavior?.Dispose();
    }

    private void ValidateIncrementalAspectValue(UiProperty property)
    {
        VerifyMutationAccess();
        ValidateLocalAspectProperty(property);
    }

    private void InvalidateIncrementalAspect()
    {
        VerifyMutationAccess();
        Invalidate(InvalidationFlags.Aspect, "Element aspect changed");
    }

    private void ValidateLocalAspectProperty(UiProperty property)
    {
        if (ReferenceEquals(property, AspectProperty))
        {
            throw new InvalidOperationException("A local aspect cannot assign UIElement.AspectProperty.");
        }

        if (!property.OwnerType.IsAssignableFrom(GetType()))
        {
            throw new InvalidOperationException(
                $"UI property '{property.DiagnosticName}' cannot be applied to element '{GetType().FullName}'.");
        }
    }

    private sealed class ElementAspectConsumer(UIElement owner) : IElementAspectConsumer
    {
        public void ValidateAspectValue(UiProperty property) => owner.ValidateIncrementalAspectValue(property);

        public void InvalidateAspect() => owner.InvalidateIncrementalAspect();
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

        if (options.HasFlag(UiPropertyOptions.AffectsAspect))
        {
            flags |= InvalidationFlags.Aspect;
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

    private static bool IsValidDimension(float value)
    {
        return float.IsNaN(value) || (float.IsFinite(value) && value >= 0);
    }

    private static bool IsSizeLimitProperty(UiProperty property)
    {
        return ReferenceEquals(property, MinWidthProperty) ||
            ReferenceEquals(property, MaxWidthProperty) ||
            ReferenceEquals(property, MinHeightProperty) ||
            ReferenceEquals(property, MaxHeightProperty);
    }

    private static bool IsValidMinimumDimension(float value)
    {
        return float.IsFinite(value) && value >= 0;
    }

    private static bool IsValidMaximumDimension(float value)
    {
        return float.IsPositiveInfinity(value) || (float.IsFinite(value) && value >= 0);
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

    private void IncrementRenderScopeVersion()
    {
        RenderScopeVersion++;
        IncrementPrismVisualVersion();
    }

    private void IncrementPrismVisualVersion()
    {
        PrismLocalVisualVersion = NextPrismVisualVersion(PrismLocalVisualVersion);
        IncrementPrismAggregateVersion();
    }

    private void IncrementPrismAggregateVersion()
    {
        for (UIElement? current = this;
            current is not null;
            current = current.PrismVisualParent)
        {
            current.PrismVisualVersion =
                NextPrismVisualVersion(current.PrismVisualVersion);
            if (current.isPrismVisualBoundary)
            {
                break;
            }
        }
    }

    private static long NextPrismVisualNodeId()
    {
        long value = Interlocked.Increment(ref nextPrismVisualNodeId);
        if (value <= 0)
        {
            throw new InvalidOperationException(
                "Prism visual node identifier space was exhausted.");
        }

        return value;
    }

    private static long NextPrismVisualVersion(long version)
    {
        if (version == long.MaxValue)
        {
            throw new InvalidOperationException(
                "Prism visual version space was exhausted.");
        }

        return version + 1;
    }
}

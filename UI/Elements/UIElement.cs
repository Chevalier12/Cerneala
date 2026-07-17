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
    private readonly HashSet<UiProperty> appliedLocalAspectProperties = new(ReferenceEqualityComparer.Instance);

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

    public bool IsInitialized => isInitialized;

    public UiElementId? ElementId { get; private set; }

    public ElementHandlerStore Handlers { get; }

    public CommandBindingCollection CommandBindings { get; }

    public BindingSubscriptionCollection Bindings { get; }

    public ResourceDictionary Resources { get; }

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

    internal virtual bool ActivatesOnPointerRelease => true;

    internal virtual TimeSpan? PointerRepeatDelay => null;

    internal virtual TimeSpan PointerRepeatInterval => TimeSpan.Zero;

    public float PresenceOpacity { get; private set; } = 1;

    public float PresenceScale { get; private set; } = 1;

    protected override UiPropertyMutationObserver? MutationObserver => Root?.Motion.Transactions;

    protected override void VerifyMutationAccess() => Root?.Relay.VerifyAccess();

    private bool hasPendingCommandStateRefresh;
    private bool hasPendingRenderScopeInvalidation;
    private bool hasPendingRenderContentInvalidation;
    private readonly List<IElementLifecycleBehavior> lifecycleBehaviors = [];
    private bool isInitialized;
    private int attachmentGeneration;

    internal LayoutSize? LastMeasureAvailableSize { get; set; }

    internal int LastMeasureLayoutVersion { get; set; } = -1;

    internal int LastMeasureViewportVersion { get; set; } = -1;

    private LayoutSize? previousMeasureAvailableSize;
    private LayoutSize previousMeasureDesiredSize;
    private int previousMeasureLayoutVersion = -1;
    private LayoutSize? olderMeasureAvailableSize;
    private LayoutSize olderMeasureDesiredSize;
    private int olderMeasureLayoutVersion = -1;
    private LayoutSize? oldestMeasureAvailableSize;
    private LayoutSize oldestMeasureDesiredSize;
    private int oldestMeasureLayoutVersion = -1;

    internal LayoutRect? LastArrangeFinalRect { get; set; }

    internal int LastArrangeLayoutVersion { get; set; } = -1;

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

    internal bool HasAttachedParent =>
        (LogicalParent?.Root is not null) || (VisualParent?.Root is not null);

    private void OnElementResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        UIRoot? root = Root;
        if (root is null)
        {
            Invalidate(InvalidationFlags.Resource | InvalidationFlags.Subtree, "Element resources changed");
            return;
        }

        if (root.Relay.CheckAccess())
        {
            Invalidate(InvalidationFlags.Resource | InvalidationFlags.Subtree, "Element resources changed");
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
                target.Invalidate(InvalidationFlags.Resource | InvalidationFlags.Subtree, "Element resources changed");
            }
        });
    }

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
        attachmentGeneration++;
        if (!isInitialized)
        {
            isInitialized = true;
            Initialized?.Invoke(this, EventArgs.Empty);
        }
        OnAttached();
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            behavior.Attach();
        }
        Root?.Motion.Layout.MarkAttached(this);
        Root?.Motion.Presence.MarkAttached(this);
        RaiseEvent(new RoutedEventArgs(LoadedEvent, this));
    }

    internal void ValidateLifecycleRoot(UIRoot root)
    {
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            behavior.ValidateRoot(root);
        }
    }

    internal void DetachFromRoot()
    {
        attachmentGeneration++;
        RaiseEvent(new RoutedEventArgs(UnloadedEvent, this));
        Root?.Motion.Layout.MarkDetached(this);
        Root?.Motion.Presence.MarkDetached(this);
        foreach (IElementLifecycleBehavior behavior in lifecycleBehaviors)
        {
            behavior.Detach();
        }
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
            return cached;
        }

        Root?.CountMeasureCall();
        LayoutSize desired = !UIElementVisibility.ParticipatesInLayout(this)
            ? LayoutSize.Zero
            : MeasureWithMargin(context);
        desired = context.Rounding.Round(desired);
        PreserveCurrentMeasureCache();
        SetDesiredSize(desired);
        LastMeasureAvailableSize = context.AvailableSize;
        LastMeasureLayoutVersion = LayoutVersion;
        LastMeasureViewportVersion = Root?.ViewportVersion ?? -1;
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

        // Alternate-constraint entries do not capture descendant measure state.
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
        LastMeasureViewportVersion = -1;
        previousMeasureAvailableSize = null;
        previousMeasureLayoutVersion = -1;
        olderMeasureAvailableSize = null;
        olderMeasureLayoutVersion = -1;
        oldestMeasureAvailableSize = null;
        oldestMeasureLayoutVersion = -1;
    }

    public LayoutRect Arrange(ArrangeContext context)
    {
        if (LastArrangeFinalRect == context.FinalRect &&
            LastArrangeLayoutVersion == LayoutVersion)
        {
            return ArrangedBounds;
        }

        Root?.CountArrangeCall();
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

        LayoutRect previousBounds = ArrangedBounds;
        ArrangedBounds = arrangedBounds;
        IncrementRenderVersion();
        Root?.RetainedRenderCache.InvalidateRoot();
        Root?.RenderQueue.Enqueue(this);
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
        bool hasWidth = !float.IsNaN(Width);
        bool hasHeight = !float.IsNaN(Height);
        LayoutSize constrainedSize = new(
            hasWidth ? MathF.Min(contentAvailableSize.Width, Width) : contentAvailableSize.Width,
            hasHeight ? MathF.Min(contentAvailableSize.Height, Height) : contentAvailableSize.Height);
        LayoutSize measuredSize = MeasureCore(new MeasureContext(constrainedSize, context.Rounding)).ClampNonNegative();
        LayoutSize contentDesiredSize = new(
            hasWidth ? Width : measuredSize.Width,
            hasHeight ? Height : measuredSize.Height);
        return new LayoutSize(
            contentDesiredSize.Width + margin.Horizontal,
            contentDesiredSize.Height + margin.Vertical).ClampNonNegative();
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
        LastMeasureViewportVersion = Root?.ViewportVersion ?? -1;
        SetDesiredSize(cachedDesiredSize);

        cachedAvailableSize = currentAvailableSize;
        cachedLayoutVersion = currentLayoutVersion;
        cachedDesiredSize = currentDesiredSize;
        desiredSize = DesiredSize;
        return true;
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
        bool hasWidth = !float.IsNaN(Width);
        bool hasHeight = !float.IsNaN(Height);
        float width = HorizontalAlignment == HorizontalAlignment.Stretch && !hasWidth
            ? finalRect.Width
            : Math.Min(desiredSize.Width, finalRect.Width);
        float height = VerticalAlignment == VerticalAlignment.Stretch && !hasHeight
            ? finalRect.Height
            : Math.Min(desiredSize.Height, finalRect.Height);

        float x = HorizontalAlignment switch
        {
            HorizontalAlignment.Center => finalRect.X + ((finalRect.Width - width) / 2),
            HorizontalAlignment.Right => finalRect.X + finalRect.Width - width,
            HorizontalAlignment.Stretch when hasWidth => finalRect.X + ((finalRect.Width - width) / 2),
            _ => finalRect.X
        };

        float y = VerticalAlignment switch
        {
            VerticalAlignment.Center => finalRect.Y + ((finalRect.Height - height) / 2),
            VerticalAlignment.Bottom => finalRect.Y + finalRect.Height - height,
            VerticalAlignment.Stretch when hasHeight => finalRect.Y + ((finalRect.Height - height) / 2),
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
        bool isVisibilityChange = ReferenceEquals(args.Property, VisibilityProperty) &&
            args.OldValue is Visibility &&
            args.NewValue is Visibility;
        Visibility oldVisibility = isVisibilityChange ? (Visibility)args.OldValue! : Visibility.Visible;
        Visibility newVisibility = isVisibilityChange ? (Visibility)args.NewValue! : Visibility.Visible;
        bool layoutParticipationChanged = isVisibilityChange &&
            (oldVisibility == Visibility.Collapsed) != (newVisibility == Visibility.Collapsed);
        bool expandingFromCollapsed = layoutParticipationChanged &&
            oldVisibility == Visibility.Collapsed;
        bool requiresExpandedSubtreeRelayout = expandingFromCollapsed &&
            (Root is not UIRoot root || !root.Scheduler.IsProcessingLayout);
        if (requiresExpandedSubtreeRelayout)
        {
            foreach (UIElement descendant in ElementTreeWalker.Descendants(this, ElementChildRole.Visual))
            {
                descendant.IncrementLayoutVersion();
            }

            flags |= InvalidationFlags.Subtree;
        }

        if (flags != InvalidationFlags.None)
        {
            Invalidate(new InvalidationRequest(this, flags, "Property changed", args.Property));
            if (requiresExpandedSubtreeRelayout &&
                Root is UIRoot layoutRoot &&
                VisualParent is UIElement parent)
            {
                layoutRoot.LayoutQueue.RequireMeasure(parent);
                layoutRoot.LayoutQueue.RequireArrange(parent);
            }
        }
    }

    private void ApplyLocalAspect(ElementAspect? aspect)
    {
        foreach (UiProperty property in appliedLocalAspectProperties)
        {
            ClearValueUntyped(property, UiPropertyValueSource.LocalAspectBase);
        }

        appliedLocalAspectProperties.Clear();
        if (aspect is null)
        {
            return;
        }

        foreach (ElementAspectValue value in aspect.DefaultValues)
        {
            if (ReferenceEquals(value.Property, AspectProperty))
            {
                throw new InvalidOperationException("A local aspect cannot assign UIElement.AspectProperty.");
            }

            if (!value.Property.OwnerType.IsAssignableFrom(GetType()))
            {
                throw new InvalidOperationException(
                    $"UI property '{value.Property.DiagnosticName}' cannot be applied to element '{GetType().FullName}'.");
            }

            SetValueUntyped(value.Property, value.Value, UiPropertyValueSource.LocalAspectBase);
            appliedLocalAspectProperties.Add(value.Property);
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

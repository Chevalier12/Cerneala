using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Elements;

public class UIElement : UiObject, IUiPropertyOwner
{
    public static readonly UiProperty<bool> IsEnabledProperty = UiProperty<bool>.Register(
        nameof(IsEnabled),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsVisibleProperty = UiProperty<bool>.Register(
        nameof(IsVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public UIElement()
    {
        LogicalChildren = new UIElementCollection(this, ElementChildRole.Logical);
        VisualChildren = new UIElementCollection(this, ElementChildRole.Visual);
    }

    public UIElement? LogicalParent { get; private set; }

    public UIElement? VisualParent { get; private set; }

    public UIElementCollection LogicalChildren { get; }

    public UIElementCollection VisualChildren { get; }

    public UIRoot? Root { get; private set; }

    public bool IsAttached => Root is not null;

    public UiElementId? ElementId { get; private set; }

    public ElementHandlerStore Handlers { get; } = new();

    public DirtyState DirtyState { get; } = new();

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
            flags |= InvalidationFlags.Render;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsInputVisual))
        {
            flags |= InvalidationFlags.InputVisual;
        }

        return flags;
    }
}

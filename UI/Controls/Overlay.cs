using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class Overlay : Control
{
    private readonly ContentPresenter presenter = new();
    private bool isProjected;

    public Overlay()
    {
        IsHitTestVisible = false;
        LogicalChildren.Add(presenter);
    }

    public static readonly RoutedEvent OpenedEvent = RoutedEventRegistry.Register(
        nameof(Opened),
        typeof(Overlay),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent ClosedEvent = RoutedEventRegistry.Register(
        nameof(Closed),
        typeof(Overlay),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(Overlay),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            ContentControl.ContentEqualityComparer));

    public static readonly UiProperty<bool> IsOpenProperty = UiProperty<bool>.Register(
        nameof(IsOpen),
        typeof(Overlay),
        new UiPropertyMetadata<bool>(
            false,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<UIElement?> PlacementTargetProperty = UiProperty<UIElement?>.Register(
        nameof(PlacementTarget),
        typeof(Overlay),
        new UiPropertyMetadata<UIElement?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsHitTest));

    public static readonly UiProperty<OverlayPlacement> PlacementProperty = UiProperty<OverlayPlacement>.Register(
        nameof(Placement),
        typeof(Overlay),
        new UiPropertyMetadata<OverlayPlacement>(
            OverlayPlacement.Auto,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange,
            validateValue: Enum.IsDefined));

    public static readonly UiProperty<bool> IsLightDismissEnabledProperty = UiProperty<bool>.Register(
        nameof(IsLightDismissEnabled),
        typeof(Overlay),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.None));

    public static readonly UiProperty<bool> MatchTargetWidthProperty = UiProperty<bool>.Register(
        nameof(MatchTargetWidth),
        typeof(Overlay),
        new UiPropertyMetadata<bool>(
            false,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange));

    public new static readonly UiProperty<float> HeightProperty = UiProperty<float>.Register(
        nameof(Height),
        typeof(Overlay),
        new UiPropertyMetadata<float>(
            float.NaN,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange,
            validateValue: value => float.IsNaN(value) || (float.IsFinite(value) && value >= 0)));

    public static readonly UiProperty<float> MaxHeightProperty = UiProperty<float>.Register(
        nameof(MaxHeight),
        typeof(Overlay),
        new UiPropertyMetadata<float>(
            float.PositiveInfinity,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange,
            validateValue: value => value >= 0 && !float.IsNaN(value)));

    public event RoutedEventHandler Opened
    {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    public object? Content
    {
        get => GetValue(ContentProperty);
        set
        {
            object? previous = Content;
            if (ContentControl.ContentEqualityComparer.Equals(previous, value))
            {
                SetValue(ContentProperty, value);
                return;
            }

            presenter.Content = value;
            try
            {
                SetValue(ContentProperty, value);
            }
            catch
            {
                presenter.Content = previous;
                throw;
            }
        }
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public UIElement? PlacementTarget
    {
        get => GetValue(PlacementTargetProperty);
        set => SetValue(PlacementTargetProperty, value);
    }

    public OverlayPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public bool IsLightDismissEnabled
    {
        get => GetValue(IsLightDismissEnabledProperty);
        set => SetValue(IsLightDismissEnabledProperty, value);
    }

    public bool MatchTargetWidth
    {
        get => GetValue(MatchTargetWidthProperty);
        set => SetValue(MatchTargetWidthProperty, value);
    }

    public new float Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public float MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }

    internal ContentPresenter ProjectedPresenter => presenter;

    internal UIElement EffectivePlacementTarget => PlacementTarget ?? this;

    internal bool IsProjected => isProjected;

    internal void SetProjected(bool value)
    {
        if (isProjected == value)
        {
            return;
        }

        isProjected = value;
        RaiseEvent(new RoutedEventArgs(value ? OpenedEvent : ClosedEvent, this));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ContentProperty) &&
            !ContentControl.ContentEqualityComparer.Equals(presenter.Content, Content))
        {
            presenter.Content = Content;
        }

        if (ReferenceEquals(args.Property, IsOpenProperty))
        {
            SynchronizeProjection();
            return;
        }

        if (ReferenceEquals(args.Property, PlacementTargetProperty) ||
            ReferenceEquals(args.Property, PlacementProperty) ||
            ReferenceEquals(args.Property, MatchTargetWidthProperty) ||
            ReferenceEquals(args.Property, HeightProperty) ||
            ReferenceEquals(args.Property, MaxHeightProperty))
        {
            Root?.OverlayManager.InvalidatePlacement(this);
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        SynchronizeProjection();
    }

    protected override void OnDetached()
    {
        if (isProjected)
        {
            Root?.OverlayManager.Hide(this);
        }

        if (IsOpen)
        {
            SetValue(IsOpenProperty, false);
        }

        base.OnDetached();
    }

    private void SynchronizeProjection()
    {
        if (Root is not UIRoot root)
        {
            return;
        }

        if (IsOpen)
        {
            root.OverlayManager.Show(this);
        }
        else
        {
            root.OverlayManager.Hide(this);
        }
    }
}

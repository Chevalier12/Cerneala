using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public class ToolTip : Control
{
    private readonly PopupRoot popupRoot = new();
    private readonly Overlay overlay;
    private object? content;

    public ToolTip()
    {
        overlay = new Overlay
        {
            Content = popupRoot,
            PlacementTarget = this,
            Placement = OverlayPlacement.Auto,
            IsLightDismissEnabled = false
        };
        overlay.Opened += OnOverlayOpened;
        overlay.Closed += OnOverlayClosed;
        LogicalChildren.Add(overlay);
        VisualChildren.Add(overlay);
    }

    public static readonly RoutedEvent OpenedEvent = RoutedEventRegistry.Register(nameof(Opened), typeof(ToolTip), RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ClosedEvent = RoutedEventRegistry.Register(nameof(Closed), typeof(ToolTip), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event RoutedEventHandler Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }
    public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

    public static readonly UiProperty<bool> IsOpenProperty = UiProperty<bool>.Register(
        nameof(IsOpen),
        typeof(ToolTip),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public object? Content
    {
        get => content;
        set
        {
            if (ContentControl.ContentEqualityComparer.Equals(content, value))
            {
                content = value;
                popupRoot.Content = value;
                return;
            }

            object? oldContent = content;
            try
            {
                popupRoot.Content = value;
                content = value;
            }
            catch
            {
                content = oldContent;
                throw;
            }
        }
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public PopupRoot PopupRoot => popupRoot;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        overlay.Measure(context);
        return LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        overlay.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsOpenProperty))
        {
            overlay.IsOpen = IsOpen;
        }
    }

    private void OnOverlayOpened(UiElementId _, RoutedEventArgs args)
    {
        if (!IsOpen)
        {
            SetValue(IsOpenProperty, true);
        }

        RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
    }

    private void OnOverlayClosed(UiElementId _, RoutedEventArgs args)
    {
        if (IsOpen)
        {
            SetValue(IsOpenProperty, false);
        }

        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }
}

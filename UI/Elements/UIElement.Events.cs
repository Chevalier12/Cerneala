using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Elements;

public partial class UIElement
{
    private readonly Dictionary<(RoutedEvent RoutedEvent, Delegate Handler), Stack<RoutedEventHandler>> typedHandlerAdapters = [];
    public static readonly RoutedEvent LoadedEvent = RoutedEventRegistry.Register(nameof(Loaded), typeof(UIElement), RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent UnloadedEvent = RoutedEventRegistry.Register(nameof(Unloaded), typeof(UIElement), RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent SizeChangedEvent = RoutedEventRegistry.Register(nameof(SizeChanged), typeof(UIElement), RoutingStrategy.Direct, typeof(SizeChangedEventArgs));

    public static readonly RoutedEvent PreviewMouseDownEvent = InputEvents.PreviewMouseDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseDownEvent = InputEvents.MouseDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseUpEvent = InputEvents.PreviewMouseUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseUpEvent = InputEvents.MouseUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseMoveEvent = InputEvents.PreviewMouseMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseMoveEvent = InputEvents.MouseMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseWheelEvent = InputEvents.PreviewMouseWheelEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseWheelEvent = InputEvents.MouseWheelEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseEnterEvent = InputEvents.MouseEnterEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseLeaveEvent = InputEvents.MouseLeaveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GotMouseCaptureEvent = InputEvents.GotMouseCaptureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent LostMouseCaptureEvent = InputEvents.LostMouseCaptureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewKeyDownEvent = InputEvents.PreviewKeyDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent KeyDownEvent = InputEvents.KeyDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewKeyUpEvent = InputEvents.PreviewKeyUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent KeyUpEvent = InputEvents.KeyUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewGotKeyboardFocusEvent = InputEvents.PreviewGotKeyboardFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GotKeyboardFocusEvent = InputEvents.GotKeyboardFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewLostKeyboardFocusEvent = InputEvents.PreviewLostKeyboardFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent LostKeyboardFocusEvent = InputEvents.LostKeyboardFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GotFocusEvent = InputEvents.GotFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent LostFocusEvent = InputEvents.LostFocusEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewTextInputEvent = InputEvents.PreviewTextInputEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TextInputEvent = InputEvents.TextInputEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent DragEnterEvent = InputEvents.DragEnterEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent DragLeaveEvent = InputEvents.DragLeaveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent DragOverEvent = InputEvents.DragOverEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent DropEvent = InputEvents.DropEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GiveFeedbackEvent = InputEvents.GiveFeedbackEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent QueryContinueDragEvent = InputEvents.QueryContinueDragEvent.AddOwner(typeof(UIElement));

    public event EventHandler? Initialized;
    public event EventHandler<UiPropertyChangedEventArgs>? DataContextChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? IsEnabledChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? IsVisibleChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? FocusableChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? IsMouseDirectlyOverChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? IsKeyboardFocusedChanged;
    public event EventHandler<UiPropertyChangedEventArgs>? IsKeyboardFocusWithinChanged;

    public event RoutedEventHandler Loaded { add => AddHandler(LoadedEvent, value); remove => RemoveHandler(LoadedEvent, value); }
    public event RoutedEventHandler Unloaded { add => AddHandler(UnloadedEvent, value); remove => RemoveHandler(UnloadedEvent, value); }
    public event RoutedEventHandler SizeChanged { add => AddHandler(SizeChangedEvent, value); remove => RemoveHandler(SizeChangedEvent, value); }
    public event RoutedEventHandler PreviewMouseDown { add => AddHandler(PreviewMouseDownEvent, value); remove => RemoveHandler(PreviewMouseDownEvent, value); }
    public event RoutedEventHandler MouseDown { add => AddHandler(MouseDownEvent, value); remove => RemoveHandler(MouseDownEvent, value); }
    public event RoutedEventHandler PreviewMouseUp { add => AddHandler(PreviewMouseUpEvent, value); remove => RemoveHandler(PreviewMouseUpEvent, value); }
    public event RoutedEventHandler MouseUp { add => AddHandler(MouseUpEvent, value); remove => RemoveHandler(MouseUpEvent, value); }
    public event RoutedEventHandler PreviewMouseMove { add => AddHandler(PreviewMouseMoveEvent, value); remove => RemoveHandler(PreviewMouseMoveEvent, value); }
    public event RoutedEventHandler MouseMove { add => AddHandler(MouseMoveEvent, value); remove => RemoveHandler(MouseMoveEvent, value); }
    public event RoutedEventHandler PreviewMouseWheel { add => AddHandler(PreviewMouseWheelEvent, value); remove => RemoveHandler(PreviewMouseWheelEvent, value); }
    public event RoutedEventHandler MouseWheel { add => AddHandler(MouseWheelEvent, value); remove => RemoveHandler(MouseWheelEvent, value); }
    public event RoutedEventHandler MouseEnter { add => AddHandler(MouseEnterEvent, value); remove => RemoveHandler(MouseEnterEvent, value); }
    public event RoutedEventHandler MouseLeave { add => AddHandler(MouseLeaveEvent, value); remove => RemoveHandler(MouseLeaveEvent, value); }
    public event RoutedEventHandler GotMouseCapture { add => AddHandler(GotMouseCaptureEvent, value); remove => RemoveHandler(GotMouseCaptureEvent, value); }
    public event RoutedEventHandler LostMouseCapture { add => AddHandler(LostMouseCaptureEvent, value); remove => RemoveHandler(LostMouseCaptureEvent, value); }
    public event RoutedEventHandler PreviewKeyDown { add => AddHandler(PreviewKeyDownEvent, value); remove => RemoveHandler(PreviewKeyDownEvent, value); }
    public event RoutedEventHandler KeyDown { add => AddHandler(KeyDownEvent, value); remove => RemoveHandler(KeyDownEvent, value); }
    public event RoutedEventHandler PreviewKeyUp { add => AddHandler(PreviewKeyUpEvent, value); remove => RemoveHandler(PreviewKeyUpEvent, value); }
    public event RoutedEventHandler KeyUp { add => AddHandler(KeyUpEvent, value); remove => RemoveHandler(KeyUpEvent, value); }
    public event RoutedEventHandler PreviewGotKeyboardFocus { add => AddHandler(PreviewGotKeyboardFocusEvent, value); remove => RemoveHandler(PreviewGotKeyboardFocusEvent, value); }
    public event RoutedEventHandler GotKeyboardFocus { add => AddHandler(GotKeyboardFocusEvent, value); remove => RemoveHandler(GotKeyboardFocusEvent, value); }
    public event RoutedEventHandler PreviewLostKeyboardFocus { add => AddHandler(PreviewLostKeyboardFocusEvent, value); remove => RemoveHandler(PreviewLostKeyboardFocusEvent, value); }
    public event RoutedEventHandler LostKeyboardFocus { add => AddHandler(LostKeyboardFocusEvent, value); remove => RemoveHandler(LostKeyboardFocusEvent, value); }
    public event RoutedEventHandler GotFocus { add => AddHandler(GotFocusEvent, value); remove => RemoveHandler(GotFocusEvent, value); }
    public event RoutedEventHandler LostFocus { add => AddHandler(LostFocusEvent, value); remove => RemoveHandler(LostFocusEvent, value); }
    public event RoutedEventHandler PreviewTextInput { add => AddHandler(PreviewTextInputEvent, value); remove => RemoveHandler(PreviewTextInputEvent, value); }
    public event RoutedEventHandler TextInput { add => AddHandler(TextInputEvent, value); remove => RemoveHandler(TextInputEvent, value); }
    public event RoutedEventHandler DragEnter { add => AddHandler(DragEnterEvent, value); remove => RemoveHandler(DragEnterEvent, value); }
    public event RoutedEventHandler DragLeave { add => AddHandler(DragLeaveEvent, value); remove => RemoveHandler(DragLeaveEvent, value); }
    public event RoutedEventHandler DragOver { add => AddHandler(DragOverEvent, value); remove => RemoveHandler(DragOverEvent, value); }
    public event RoutedEventHandler Drop { add => AddHandler(DropEvent, value); remove => RemoveHandler(DropEvent, value); }
    public event RoutedEventHandler GiveFeedback { add => AddHandler(GiveFeedbackEvent, value); remove => RemoveHandler(GiveFeedbackEvent, value); }
    public event RoutedEventHandler QueryContinueDrag { add => AddHandler(QueryContinueDragEvent, value); remove => RemoveHandler(QueryContinueDragEvent, value); }

    public void AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler, bool handledEventsToo = false)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);
        Handlers.AddHandler(routedEvent, handler, handledEventsToo);
    }

    public void RemoveHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
    {
        Handlers.RemoveHandler(routedEvent, handler);
    }

    public void RaiseEvent(RoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.RoutedEvent is null)
        {
            throw new ArgumentException("RoutedEvent must be assigned before raising the event.", nameof(args));
        }

        if (!args.RoutedEvent.ArgsType.IsInstanceOfType(args))
        {
            throw new ArgumentException($"Event '{args.RoutedEvent.Name}' requires args assignable to {args.RoutedEvent.ArgsType.Name}.", nameof(args));
        }

        args.OriginalSource ??= this;
        args.Source ??= args.OriginalSource;

        if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct || Root is null || !ElementId.HasValue)
        {
            foreach (RoutedEventHandlerRegistration registration in Handlers.GetRegistrations(args.RoutedEvent))
            {
                if (!args.Handled || registration.HandledEventsToo)
                {
                    registration.Handler(ElementId ?? new UiElementId(GetType().Name), args);
                }
            }

            return;
        }

        ElementInputRouteMap routeMap = Root.InputCache.EnsureCurrent(Root);
        if (routeMap.TryGetId(this, out UiElementId id))
        {
            RoutedEventRouter.Raise(routeMap.InputTree, id, args);
        }
    }

    protected void AddTypedHandler<TEventArgs>(RoutedEvent routedEvent, EventHandler<TEventArgs> handler)
        where TEventArgs : RoutedEventArgs
    {
        ArgumentNullException.ThrowIfNull(handler);
        RoutedEventHandler adapter = (_, args) => handler(this, (TEventArgs)args);
        (RoutedEvent, Delegate) key = (routedEvent, handler);
        if (!typedHandlerAdapters.TryGetValue(key, out Stack<RoutedEventHandler>? adapters))
        {
            adapters = new Stack<RoutedEventHandler>();
            typedHandlerAdapters.Add(key, adapters);
        }

        adapters.Push(adapter);
        AddHandler(routedEvent, adapter);
    }

    protected void RemoveTypedHandler<TEventArgs>(RoutedEvent routedEvent, EventHandler<TEventArgs> handler)
        where TEventArgs : RoutedEventArgs
    {
        (RoutedEvent, Delegate) key = (routedEvent, handler);
        if (!typedHandlerAdapters.TryGetValue(key, out Stack<RoutedEventHandler>? adapters) || adapters.Count == 0)
        {
            return;
        }

        RemoveHandler(routedEvent, adapters.Pop());
        if (adapters.Count == 0)
        {
            typedHandlerAdapters.Remove(key);
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, AspectProperty)) ApplyLocalAspect(Aspect);
        else if (ReferenceEquals(args.Property, DataContextProperty)) DataContextChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, IsEnabledProperty)) IsEnabledChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, IsVisibleProperty) || ReferenceEquals(args.Property, VisibilityProperty)) IsVisibleChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, FocusableProperty)) FocusableChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, IsPointerOverProperty)) IsMouseDirectlyOverChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, IsKeyboardFocusedProperty)) IsKeyboardFocusedChanged?.Invoke(this, args);
        else if (ReferenceEquals(args.Property, IsKeyboardFocusWithinProperty)) IsKeyboardFocusWithinChanged?.Invoke(this, args);
    }
}

public sealed class SizeChangedEventArgs : RoutedEventArgs
{
    public SizeChangedEventArgs(RoutedEvent routedEvent, object source, LayoutSize previousSize, LayoutSize newSize)
        : base(routedEvent, source)
    {
        PreviousSize = previousSize;
        NewSize = newSize;
    }

    public LayoutSize PreviousSize { get; }
    public LayoutSize NewSize { get; }
    public bool WidthChanged => PreviousSize.Width != NewSize.Width;
    public bool HeightChanged => PreviousSize.Height != NewSize.Height;
}

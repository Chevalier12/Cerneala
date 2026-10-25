using System.Runtime.ExceptionServices;
using Cerneala.UI.Aspect;
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
    public static readonly RoutedEvent RequestBringIntoViewEvent = RoutedEventRegistry.Register(nameof(RequestBringIntoView), typeof(UIElement), RoutingStrategy.Bubble, typeof(RequestBringIntoViewEventArgs));

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
    public static readonly RoutedEvent PreviewInputHeldEvent = InputEvents.PreviewInputHeldEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent InputHeldEvent = InputEvents.InputHeldEvent.AddOwner(typeof(UIElement));
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
    public event RoutedEventHandler<SizeChangedEventArgs> SizeChanged { add => AddHandler(SizeChangedEvent, value); remove => RemoveHandler(SizeChangedEvent, value); }
    public event EventHandler<RequestBringIntoViewEventArgs> RequestBringIntoView { add => AddTypedHandler(RequestBringIntoViewEvent, value); remove => RemoveTypedHandler(RequestBringIntoViewEvent, value); }
    public event RoutedEventHandler<MouseButtonEventArgs> PreviewMouseDown { add => AddHandler(PreviewMouseDownEvent, value); remove => RemoveHandler(PreviewMouseDownEvent, value); }
    public event RoutedEventHandler<MouseButtonEventArgs> MouseDown { add => AddHandler(MouseDownEvent, value); remove => RemoveHandler(MouseDownEvent, value); }
    public event RoutedEventHandler<MouseButtonEventArgs> PreviewMouseUp { add => AddHandler(PreviewMouseUpEvent, value); remove => RemoveHandler(PreviewMouseUpEvent, value); }
    public event RoutedEventHandler<MouseButtonEventArgs> MouseUp { add => AddHandler(MouseUpEvent, value); remove => RemoveHandler(MouseUpEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> PreviewMouseMove { add => AddHandler(PreviewMouseMoveEvent, value); remove => RemoveHandler(PreviewMouseMoveEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> MouseMove { add => AddHandler(MouseMoveEvent, value); remove => RemoveHandler(MouseMoveEvent, value); }
    public event RoutedEventHandler<MouseWheelEventArgs> PreviewMouseWheel { add => AddHandler(PreviewMouseWheelEvent, value); remove => RemoveHandler(PreviewMouseWheelEvent, value); }
    public event RoutedEventHandler<MouseWheelEventArgs> MouseWheel { add => AddHandler(MouseWheelEvent, value); remove => RemoveHandler(MouseWheelEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> MouseEnter { add => AddHandler(MouseEnterEvent, value); remove => RemoveHandler(MouseEnterEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> MouseLeave { add => AddHandler(MouseLeaveEvent, value); remove => RemoveHandler(MouseLeaveEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> GotMouseCapture { add => AddHandler(GotMouseCaptureEvent, value); remove => RemoveHandler(GotMouseCaptureEvent, value); }
    public event RoutedEventHandler<MouseEventArgs> LostMouseCapture { add => AddHandler(LostMouseCaptureEvent, value); remove => RemoveHandler(LostMouseCaptureEvent, value); }
    public event RoutedEventHandler<KeyEventArgs> PreviewKeyDown { add => AddHandler(PreviewKeyDownEvent, value); remove => RemoveHandler(PreviewKeyDownEvent, value); }
    public event RoutedEventHandler<KeyEventArgs> KeyDown { add => AddHandler(KeyDownEvent, value); remove => RemoveHandler(KeyDownEvent, value); }
    public event RoutedEventHandler<KeyEventArgs> PreviewKeyUp { add => AddHandler(PreviewKeyUpEvent, value); remove => RemoveHandler(PreviewKeyUpEvent, value); }
    public event RoutedEventHandler<KeyEventArgs> KeyUp { add => AddHandler(KeyUpEvent, value); remove => RemoveHandler(KeyUpEvent, value); }
    public event RoutedEventHandler<InputStateEventArgs> PreviewInputHeld { add => AddHandler(PreviewInputHeldEvent, value); remove => RemoveHandler(PreviewInputHeldEvent, value); }
    public event RoutedEventHandler<InputStateEventArgs> InputHeld { add => AddHandler(InputHeldEvent, value); remove => RemoveHandler(InputHeldEvent, value); }
    public event RoutedEventHandler<KeyboardFocusChangedEventArgs> PreviewGotKeyboardFocus { add => AddHandler(PreviewGotKeyboardFocusEvent, value); remove => RemoveHandler(PreviewGotKeyboardFocusEvent, value); }
    public event RoutedEventHandler<KeyboardFocusChangedEventArgs> GotKeyboardFocus { add => AddHandler(GotKeyboardFocusEvent, value); remove => RemoveHandler(GotKeyboardFocusEvent, value); }
    public event RoutedEventHandler<KeyboardFocusChangedEventArgs> PreviewLostKeyboardFocus { add => AddHandler(PreviewLostKeyboardFocusEvent, value); remove => RemoveHandler(PreviewLostKeyboardFocusEvent, value); }
    public event RoutedEventHandler<KeyboardFocusChangedEventArgs> LostKeyboardFocus { add => AddHandler(LostKeyboardFocusEvent, value); remove => RemoveHandler(LostKeyboardFocusEvent, value); }
    public event RoutedEventHandler GotFocus { add => AddHandler(GotFocusEvent, value); remove => RemoveHandler(GotFocusEvent, value); }
    public event RoutedEventHandler LostFocus { add => AddHandler(LostFocusEvent, value); remove => RemoveHandler(LostFocusEvent, value); }
    public event RoutedEventHandler<TextCompositionEventArgs> PreviewTextInput { add => AddHandler(PreviewTextInputEvent, value); remove => RemoveHandler(PreviewTextInputEvent, value); }
    public event RoutedEventHandler<TextCompositionEventArgs> TextInput { add => AddHandler(TextInputEvent, value); remove => RemoveHandler(TextInputEvent, value); }
    public event RoutedEventHandler<DragEventArgs> DragEnter { add => AddHandler(DragEnterEvent, value); remove => RemoveHandler(DragEnterEvent, value); }
    public event RoutedEventHandler<DragEventArgs> DragLeave { add => AddHandler(DragLeaveEvent, value); remove => RemoveHandler(DragLeaveEvent, value); }
    public event RoutedEventHandler<DragEventArgs> DragOver { add => AddHandler(DragOverEvent, value); remove => RemoveHandler(DragOverEvent, value); }
    public event RoutedEventHandler<DragEventArgs> Drop { add => AddHandler(DropEvent, value); remove => RemoveHandler(DropEvent, value); }
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

    // The handler receives the event's args as TArgs. TArgs must accept the event's ArgsType, so the cast in the
    // adapter cannot fail: MouseDownEvent (MouseButtonEventArgs) accepts MouseButtonEventArgs, MouseEventArgs or
    // RoutedEventArgs, and refuses KeyEventArgs.
    public void AddHandler<TArgs>(RoutedEvent routedEvent, RoutedEventHandler<TArgs> handler, bool handledEventsToo = false)
        where TArgs : RoutedEventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);
        if (!typeof(TArgs).IsAssignableFrom(routedEvent.ArgsType))
        {
            throw new ArgumentException($"Event '{routedEvent.Name}' raises {routedEvent.ArgsType.Name}, which is not assignable to {typeof(TArgs).Name}.", nameof(handler));
        }

        AddAdaptedHandler(routedEvent, handler, (sender, args) => handler(sender, (TArgs)args), handledEventsToo);
    }

    public void RemoveHandler<TArgs>(RoutedEvent routedEvent, RoutedEventHandler<TArgs> handler)
        where TArgs : RoutedEventArgs
    {
        RemoveAdaptedHandler(routedEvent, handler);
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

        if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct || Root is null)
        {
            RaiseLocalHandlers(args);
            return;
        }

        ElementInputRouteMap routeMap = Root.InputCache.EnsureCurrent(Root);
        if (routeMap.Contains(this))
        {
            RoutedEventRouter.Raise(routeMap.InputTree, this, args);
            return;
        }

        RaiseLocalHandlers(args);
    }

    // As WPF's FrameworkElement.BringIntoView: raises RequestBringIntoView from this element, so the ScrollViewers
    // around it scroll until the whole element is visible. It uses the positions of the last arrange.
    public void BringIntoView()
    {
        BringIntoView(LayoutRect.Empty);
    }

    // As above for targetRectangle, in this element's coordinates, where 0, 0 is its top left corner. LayoutRect.Empty
    // means the whole element.
    public void BringIntoView(LayoutRect targetRectangle)
    {
        RaiseEvent(new RequestBringIntoViewEventArgs(RequestBringIntoViewEvent, this, targetRectangle));
    }

    protected void AddTypedHandler<TEventArgs>(RoutedEvent routedEvent, EventHandler<TEventArgs> handler)
        where TEventArgs : RoutedEventArgs
    {
        ArgumentNullException.ThrowIfNull(handler);
        AddAdaptedHandler(routedEvent, handler, (_, args) => handler(this, (TEventArgs)args), handledEventsToo: false);
    }

    protected void RemoveTypedHandler<TEventArgs>(RoutedEvent routedEvent, EventHandler<TEventArgs> handler)
        where TEventArgs : RoutedEventArgs
    {
        RemoveAdaptedHandler(routedEvent, handler);
    }

    // A typed handler is stored as an untyped adapter. The adapters of one (event, handler) pair are kept on a stack,
    // so removing the handler removes exactly one of its registrations.
    private void AddAdaptedHandler(RoutedEvent routedEvent, Delegate handler, RoutedEventHandler adapter, bool handledEventsToo)
    {
        (RoutedEvent, Delegate) key = (routedEvent, handler);
        if (!typedHandlerAdapters.TryGetValue(key, out Stack<RoutedEventHandler>? adapters))
        {
            adapters = new Stack<RoutedEventHandler>();
            typedHandlerAdapters.Add(key, adapters);
        }

        adapters.Push(adapter);
        AddHandler(routedEvent, adapter, handledEventsToo);
    }

    private void RemoveAdaptedHandler(RoutedEvent routedEvent, Delegate handler)
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

    private void RaiseLocalHandlers(RoutedEventArgs args)
    {
        args.Source = this;
        foreach (RoutedEventHandlerRegistration registration in Handlers.GetRegistrations(args.RoutedEvent))
        {
            if (!args.Handled || registration.HandledEventsToo)
            {
                registration.Handler(this, args);
            }
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (ReferenceEquals(args.Property, AspectProperty))
        {
            // The new aspect is already stored, so a throwing PropertyChanged handler or a failing behavior lifetime
            // does not skip the switch: the element follows the aspect it reports, then the first failure propagates.
            ExceptionDispatchInfo? failure = null;
            try { base.OnPropertyChanged(args); }
            catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
            FollowAspect(ref failure);
            failure?.Throw();
            return;
        }

        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, DataContextProperty)) DataContextChanged?.Invoke(this, args);
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

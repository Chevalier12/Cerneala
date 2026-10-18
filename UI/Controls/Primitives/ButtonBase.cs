using Cerneala.UI.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public class ButtonBase : ContentControl, IInputPressable, IInputCommandSource, ICommandStateSource, IInputActivatable
{
    private readonly CommandSourceState commandState;

    public static readonly UiProperty<bool> IsPressedProperty = UiProperty<bool>.Register(
        nameof(IsPressed),
        typeof(ButtonBase),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<ICommand?> CommandProperty = UiProperty<ICommand?>.Register(
        nameof(Command),
        typeof(ButtonBase),
        new UiPropertyMetadata<ICommand?>(null, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<object?> CommandParameterProperty = UiProperty<object?>.Register(
        nameof(CommandParameter),
        typeof(ButtonBase),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.None));

    public ButtonBase()
    {
        commandState = new CommandSourceState(this, () => Command, () => CommandParameter);
        Focusable = true;
        IsTabStop = true;
        Cursor = Cerneala.UI.Input.Cursor.Hand;
        AddHandler(MouseUpEvent, OnMouseUp);
    }

    public static readonly RoutedEvent ClickEvent = RoutedEventRegistry.Register(nameof(Click), typeof(ButtonBase), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event RoutedEventHandler Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

    public bool IsPressed
    {
        get => GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected virtual void OnClick()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    protected virtual bool ShouldClickOnMouseUp => true;

    internal override bool ActivatesOnPointerRelease => ShouldClickOnMouseUp;

    void IInputActivatable.Activate()
    {
        OnClick();
    }

    public bool CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        return commandState.CanExecute(router, routeMap);
    }

    public bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        return commandState.Execute(router, routeMap);
    }

    public bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)
    {
        return commandState.Refresh(router, routeMap);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        commandState.Attach();
    }

    protected override void OnDetached()
    {
        commandState.Detach();
        base.OnDetached();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, CommandProperty))
        {
            commandState.OnCommandChanged();
        }
        else if (ReferenceEquals(args.Property, CommandParameterProperty))
        {
            commandState.OnParameterChanged();
        }
    }

    private void OnMouseUp(UiElementId _, RoutedEventArgs args)
    {
        if (ShouldClickOnMouseUp &&
            args is MouseButtonEventArgs { ChangedButton: InputMouseButton.Left, ClickCount: > 0 } &&
            OwnsClick(args.OriginalSource))
        {
            OnClick();
        }
    }

    // The input bridge resolves a click to the nearest command source on the route of the clicked element and runs
    // only that source's command. A click that a command source in this button's content owns, such as a Button or
    // a ComboBox's PART_DropDownToggle, bubbles its MouseUp here but is not this button's click (#421).
    private bool OwnsClick(object? originalSource)
    {
        if (Root is not UIRoot root)
        {
            return true;
        }

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        UIElement? source = originalSource switch
        {
            UIElement element => element,
            UiElementId id when routeMap.TryGetElement(id, out UIElement? resolved) => resolved,
            _ => null
        };
        if (source is null)
        {
            return true;
        }

        foreach (UIElement current in routeMap.GetRouteToRoot(source))
        {
            if (current is IInputCommandSource)
            {
                return ReferenceEquals(current, this);
            }
        }

        return true;
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_SubmenuOverlay", typeof(Overlay))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class MenuItem : ItemsControl, IInputCommandSource, ICommandStateSource, IInputActivatable
{
    private readonly CommandSourceState commandState;
    private ContentPresenter? headerPresenter;
    private Overlay? submenuOverlay;
    private UIElement? submenuIndicator;
    private OverlayDismissScope? dismissScope;
    private MenuSession? menuSession;
    private Menu? menuRoot;
    private MenuItem? parentMenuItem;
    private bool reportedSubmenuOpen;
    private bool synchronizingSessionState;
    private bool pendingLeafActivation;

    public MenuItem()
    {
        commandState = new CommandSourceState(this, () => Command, () => CommandParameter);
        Focusable = true;
        IsTabStop = true;
        Cursor = Cerneala.UI.Input.Cursor.Hand;
        ItemsPanel = new global::Cerneala.UI.Layout.Panels.StackPanel
        {
            Orientation = Orientation.Vertical
        };
        SetFrameworkDefault(BackgroundProperty, new SolidColorBrush(Color.White));
        SetFrameworkDefault(ForegroundProperty, new SolidColorBrush(Color.Black));
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(210, 214, 220)));
        SetFrameworkDefault(PaddingProperty, new Thickness(8, 5, 8, 5));
        SetFrameworkDefault(ComponentTemplateProperty, MenuTemplates.MenuItem);
        AddHandler(MouseUpEvent, OnMouseUp);
        AddHandler(MouseEnterEvent, OnMouseEnter);
        AddHandler(KeyDownEvent, OnKeyDown);
    }

    public static readonly RoutedEvent ClickEvent = RoutedEventRegistry.Register(
        nameof(Click),
        typeof(MenuItem),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent SubmenuOpenedEvent = RoutedEventRegistry.Register(
        nameof(SubmenuOpened),
        typeof(MenuItem),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent SubmenuClosedEvent = RoutedEventRegistry.Register(
        nameof(SubmenuClosed),
        typeof(MenuItem),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly UiProperty<object?> HeaderProperty = UiProperty<object?>.Register(
        nameof(Header),
        typeof(MenuItem),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            ContentControl.ContentEqualityComparer));

    public static readonly UiProperty<ICommand?> CommandProperty = UiProperty<ICommand?>.Register(
        nameof(Command),
        typeof(MenuItem),
        new UiPropertyMetadata<ICommand?>(null, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<object?> CommandParameterProperty = UiProperty<object?>.Register(
        nameof(CommandParameter),
        typeof(MenuItem),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.None));

    public static readonly UiProperty<bool> IsSubmenuOpenProperty = UiProperty<bool>.Register(
        nameof(IsSubmenuOpen),
        typeof(MenuItem),
        new UiPropertyMetadata<bool>(
            false,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest |
            UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsSemantics));

    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public event RoutedEventHandler SubmenuOpened
    {
        add => AddHandler(SubmenuOpenedEvent, value);
        remove => RemoveHandler(SubmenuOpenedEvent, value);
    }

    public event RoutedEventHandler SubmenuClosed
    {
        add => AddHandler(SubmenuClosedEvent, value);
        remove => RemoveHandler(SubmenuClosedEvent, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set
        {
            object? previous = Header;
            if (ContentControl.ContentEqualityComparer.Equals(previous, value))
            {
                SetValue(HeaderProperty, value);
                return;
            }

            if (headerPresenter is null)
            {
                SetValue(HeaderProperty, value);
                return;
            }

            headerPresenter.Content = value;
            try
            {
                SetValue(HeaderProperty, value);
            }
            catch
            {
                headerPresenter.Content = previous;
                throw;
            }
        }
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

    public bool IsSubmenuOpen
    {
        get => GetValue(IsSubmenuOpenProperty);
        set => SetValue(IsSubmenuOpenProperty, value);
    }

    internal bool HasSubmenu => ItemCount > 0;

    internal Overlay? SubmenuOverlay => submenuOverlay;

    internal MenuSession? Session => menuSession;

    internal Menu? MenuRoot => menuRoot;

    internal MenuItem? ParentMenuItem => parentMenuItem;

    protected override Type DefaultContainerType => typeof(MenuItem);

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is MenuItem ? item.GetType() : typeof(MenuItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is MenuItem menuItem ? menuItem : new MenuItem();
    }

    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is MenuItem menuItem)
        {
            MenuItemContainerPolicy.Prepare(this, menuItem, index, item);
            return;
        }

        base.PrepareItemContent(container, index, item);
    }

    protected internal override void OnItemContainerPrepared(UIElement container, int index)
    {
        base.OnItemContainerPrepared(container, index);
        if (container is MenuItem menuItem && menuSession is not null && menuRoot is not null)
        {
            menuItem.SetMenuOwner(menuSession, menuRoot, this);
            menuSession.OnContainerPrepared(this, menuItem);
        }
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        object? item = ItemContainerGenerator.GetItem(container);
        if (container is MenuItem menuItem)
        {
            menuItem.SetMenuOwner(session: null, root: null, parent: null);
            MenuItemContainerPolicy.Clear(menuItem, item);
        }

        base.ClearItemContainer(container);
    }

    public bool CanExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        return !HasSubmenu && commandState.CanExecute(router, routeMap);
    }

    public bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap)
    {
        if (HasSubmenu)
        {
            return false;
        }

        bool completesSessionActivation = pendingLeafActivation;
        try
        {
            return commandState.Execute(router, routeMap);
        }
        finally
        {
            pendingLeafActivation = false;
            if (completesSessionActivation)
            {
                menuSession?.CompleteLeafActivation(this);
            }
        }
    }

    public bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)
    {
        return commandState.Refresh(router, routeMap);
    }

    void IInputActivatable.Activate()
    {
        ActivateItem(focusFirstChild: true);
    }

    internal void ActivateItem()
    {
        ActivateItem(focusFirstChild: true);
    }

    private void ActivateItem(bool focusFirstChild)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (HasSubmenu)
        {
            if (menuSession is not null)
            {
                menuSession.ActivateParent(this, focusFirstChild);
            }
            else
            {
                IsSubmenuOpen = !IsSubmenuOpen;
            }

            return;
        }

        pendingLeafActivation = menuSession is not null;
        try
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }
        catch
        {
            pendingLeafActivation = false;
            throw;
        }
    }

    internal void SetDismissScope(OverlayDismissScope? scope)
    {
        dismissScope = scope;
        if (submenuOverlay is not null)
        {
            submenuOverlay.DismissScope = scope;
        }
    }

    internal void SetMenuOwner(MenuSession? session, Menu? root, MenuItem? parent)
    {
        if (ReferenceEquals(menuSession, session) &&
            ReferenceEquals(menuRoot, root) &&
            ReferenceEquals(parentMenuItem, parent))
        {
            return;
        }

        menuSession?.OnItemReleased(this);
        menuSession = session;
        menuRoot = root;
        parentMenuItem = parent;
        pendingLeafActivation = false;
        SetDismissScope(session?.DismissScope);
        SynchronizeMenuPlacement();
        if (session is not null && IsSubmenuOpen)
        {
            session.OnSubmenuStateRequested(this, isOpen: true);
        }
    }

    internal void SetSubmenuOpenFromSession(bool value)
    {
        if (IsSubmenuOpen == value)
        {
            return;
        }

        synchronizingSessionState = true;
        try
        {
            IsSubmenuOpen = value;
        }
        finally
        {
            synchronizingSessionState = false;
        }
    }

    internal override void OnItemsViewSourceChanged()
    {
        base.OnItemsViewSourceChanged();
        menuSession?.OnItemsChanged(this);
        ItemContainerGenerator.Clear();
        if (!HasSubmenu)
        {
            IsSubmenuOpen = false;
        }

        SynchronizeSubmenuIndicator();
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        if (instance is null)
        {
            IsSubmenuOpen = false;
            DetachTemplateParts();
            ActivateItemsPresenter(null);
            return;
        }

        DetachTemplateParts();
        ActivateItemsPresenter(null);
        headerPresenter = GetRequiredTemplatePart<ContentPresenter>("PART_HeaderPresenter");
        submenuOverlay = GetRequiredTemplatePart<Overlay>("PART_SubmenuOverlay");
        ItemsPresenter presenter = GetRequiredTemplatePart<ItemsPresenter>("PART_ItemsPresenter");
        submenuIndicator = GetOptionalTemplatePart<UIElement>("PART_SubmenuIndicator");
        ActivateItemsPresenter(presenter);
        submenuOverlay.PlacementTarget = this;
        SynchronizeMenuPlacement();
        submenuOverlay.IsLightDismissEnabled = true;
        submenuOverlay.DismissScope = dismissScope;
        submenuOverlay.Opened += OnOverlayOpened;
        submenuOverlay.Closed += OnOverlayClosed;
        headerPresenter.Content = Header;
        SynchronizeSubmenuIndicator();
        SynchronizeSubmenuState();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, HeaderProperty))
        {
            if (headerPresenter is not null &&
                !ContentControl.ContentEqualityComparer.Equals(headerPresenter.Content, Header))
            {
                headerPresenter.Content = Header;
            }
        }
        else if (ReferenceEquals(args.Property, CommandProperty))
        {
            commandState.OnCommandChanged();
        }
        else if (ReferenceEquals(args.Property, CommandParameterProperty))
        {
            commandState.OnParameterChanged();
        }
        else if (ReferenceEquals(args.Property, IsSubmenuOpenProperty))
        {
            if (IsSubmenuOpen && (!IsEnabled || !HasSubmenu))
            {
                IsSubmenuOpen = false;
                return;
            }

            SynchronizeSubmenuState();
            if (!synchronizingSessionState)
            {
                menuSession?.OnSubmenuStateRequested(this, IsSubmenuOpen);
            }
        }
        else if (ReferenceEquals(args.Property, IsEnabledProperty) && !IsEnabled)
        {
            IsSubmenuOpen = false;
            menuSession?.OnItemUnavailable(this);
        }
        else if (ReferenceEquals(args.Property, VisibilityProperty) &&
                 !UIElementVisibility.ParticipatesInInput(this))
        {
            IsSubmenuOpen = false;
            menuSession?.OnItemUnavailable(this);
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        commandState.Attach();
        SynchronizeSubmenuState();
        menuSession?.OnItemAttached(this);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect result = base.ArrangeCore(context);
        menuSession?.OnItemArranged(this);
        return result;
    }

    protected override void OnDetached()
    {
        IsSubmenuOpen = false;
        ReportSubmenuState(false);
        commandState.Detach();
        menuSession?.OnItemDetached(this);
        base.OnDetached();
    }

    private void DetachTemplateParts()
    {
        if (headerPresenter is not null)
        {
            headerPresenter.Content = null;
        }

        if (submenuOverlay is not null)
        {
            submenuOverlay.Opened -= OnOverlayOpened;
            submenuOverlay.Closed -= OnOverlayClosed;
            submenuOverlay.DismissScope = null;
            submenuOverlay.IsOpen = false;
        }

        headerPresenter = null;
        submenuOverlay = null;
        submenuIndicator = null;
    }

    private void SynchronizeSubmenuState()
    {
        if (submenuOverlay is null)
        {
            ApplyTemplate();
        }

        if (submenuOverlay is not null)
        {
            submenuOverlay.IsOpen = IsSubmenuOpen && IsEnabled && HasSubmenu;
        }
    }

    private void SynchronizeSubmenuIndicator()
    {
        if (submenuIndicator is not null)
        {
            submenuIndicator.Visibility = HasSubmenu ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SynchronizeMenuPlacement()
    {
        if (submenuOverlay is not null)
        {
            submenuOverlay.Placement = parentMenuItem is null && menuRoot is MenuBar
                ? OverlayPlacement.Bottom
                : OverlayPlacement.AutoHorizontal;
        }
    }

    private void OnOverlayOpened(UiElementId _, RoutedEventArgs args)
    {
        if (!IsEnabled || !HasSubmenu)
        {
            if (submenuOverlay is not null)
            {
                submenuOverlay.IsOpen = false;
            }

            return;
        }

        if (!IsSubmenuOpen)
        {
            SetValue(IsSubmenuOpenProperty, true);
        }

        ReportSubmenuState(true);
    }

    private void OnOverlayClosed(UiElementId _, RoutedEventArgs args)
    {
        if (IsSubmenuOpen)
        {
            SetValue(IsSubmenuOpenProperty, false);
        }

        ReportSubmenuState(false);
    }

    private void ReportSubmenuState(bool isOpen)
    {
        if (reportedSubmenuOpen == isOpen)
        {
            return;
        }

        reportedSubmenuOpen = isOpen;
        RaiseEvent(new RoutedEventArgs(isOpen ? SubmenuOpenedEvent : SubmenuClosedEvent, this));
    }

    private void OnMouseUp(UiElementId _, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs
            {
                ChangedButton: InputMouseButton.Left,
                ClickCount: > 0
            } || !IsHeaderSource(args.OriginalSource))
        {
            return;
        }

        ActivateItem(focusFirstChild: false);
    }

    private void OnMouseEnter(UiElementId _, RoutedEventArgs args)
    {
        menuSession?.OnPointerEntered(this);
    }

    private void OnKeyDown(UiElementId _, RoutedEventArgs args)
    {
        if (args is KeyEventArgs keyArgs && menuSession?.HandleKey(this, keyArgs) == true)
        {
            args.Handled = true;
        }
    }

    private bool IsHeaderSource(object? source)
    {
        UIElement? element = source switch
        {
            UIElement candidate => candidate,
            UiElementId id when Root?.ElementIds.TryGetElement(id, out UIElement? resolved) == true => resolved,
            _ => null
        };

        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }
        }

        return false;
    }
}

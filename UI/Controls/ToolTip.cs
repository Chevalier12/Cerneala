using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public class ToolTip : Control
{
    public static readonly RoutedEvent OpenedEvent = RoutedEventRegistry.Register(nameof(Opened), typeof(ToolTip), RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ClosedEvent = RoutedEventRegistry.Register(nameof(Closed), typeof(ToolTip), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event RoutedEventHandler Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }
    public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }
    private readonly PopupRoot popupRoot = new();
    private object? content;
    private bool popupAttached;

    public static readonly UiProperty<bool> IsOpenProperty = UiProperty<bool>.Register(
        nameof(IsOpen),
        typeof(ToolTip),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

    public object? Content
    {
        get => content;
        set
        {
            object? oldContent = content;
            if (ContentControl.ContentEqualityComparer.Equals(content, value))
            {
                content = value;
                if (IsOpen)
                {
                    RefreshPopupRoot();
                }

                return;
            }

            content = value;
            try
            {
                if (IsOpen)
                {
                    RefreshPopupRoot();
                }
            }
            catch
            {
                content = oldContent;
                if (IsOpen && popupAttached)
                {
                    popupRoot.Content = oldContent;
                }

                throw;
            }

            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "ToolTip content changed");
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
        RefreshPopupRoot();
        return IsOpen ? popupRoot.Measure(context) : LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RefreshPopupRoot();
        if (IsOpen)
        {
            popupRoot.Arrange(context);
        }

        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsOpenProperty))
        {
            RefreshPopupRoot();
            RaiseEvent(new RoutedEventArgs(IsOpen ? OpenedEvent : ClosedEvent, this));
        }
    }

    private void RefreshPopupRoot()
    {
        if (IsOpen)
        {
            AttachPopupRoot();
            return;
        }

        if (!popupAttached)
        {
            return;
        }

        popupRoot.Content = null;
        VisualChildren.Remove(popupRoot);
        LogicalChildren.Remove(popupRoot);
        popupAttached = false;
    }

    private void AttachPopupRoot()
    {
        if (popupAttached)
        {
            popupRoot.Content = content;
            return;
        }

        try
        {
            popupRoot.Content = content;
            LogicalChildren.Add(popupRoot);
            try
            {
                VisualChildren.Add(popupRoot);
            }
            catch
            {
                LogicalChildren.Remove(popupRoot);
                throw;
            }

            popupAttached = true;
        }
        catch
        {
            popupRoot.Content = null;
            VisualChildren.Remove(popupRoot);
            LogicalChildren.Remove(popupRoot);
            popupAttached = false;
            throw;
        }
    }
}

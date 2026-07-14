using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_ScrollContentPresenter", typeof(ScrollContentPresenter))]
[TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_VerticalScrollBar", typeof(ScrollBar))]
public class ScrollViewer : Control
{
    private const float LineScrollAmount = 48;
    private ScrollContentPresenter? presenter;
    private ScrollBar? horizontalScrollBar;
    private ScrollBar? verticalScrollBar;
    private bool syncingScrollBars;
    private object? content;

    public ScrollViewer()
    {
        Handlers.AddHandler(InputEvents.MouseWheelEvent, OnMouseWheel);
        SetValue(ComponentTemplateProperty, ScrollViewerTemplates.Default, UiPropertyValueSource.AspectBase);
    }

    public static readonly RoutedEvent ScrollChangedEvent = RoutedEventRegistry.Register(
        nameof(ScrollChanged),
        typeof(ScrollViewer),
        RoutingStrategy.Bubble,
        typeof(ScrollChangedEventArgs));

    public static readonly UiProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty = UiProperty<ScrollBarVisibility>.Register(
        nameof(HorizontalScrollBarVisibility),
        typeof(ScrollViewer),
        new UiPropertyMetadata<ScrollBarVisibility>(
            ScrollBarVisibility.Disabled,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty = UiProperty<ScrollBarVisibility>.Register(
        nameof(VerticalScrollBarVisibility),
        typeof(ScrollViewer),
        new UiPropertyMetadata<ScrollBarVisibility>(
            ScrollBarVisibility.Auto,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public event EventHandler<ScrollChangedEventArgs> ScrollChanged
    {
        add => AddTypedHandler(ScrollChangedEvent, value);
        remove => RemoveTypedHandler(ScrollChangedEvent, value);
    }

    public object? Content
    {
        get => content;
        set
        {
            if (ContentControl.ContentEqualityComparer.Equals(content, value))
            {
                content = value;
                if (presenter is not null)
                {
                    presenter.Content = value;
                }

                return;
            }

            content = value;
            if (presenter is not null)
            {
                presenter.Content = value;
            }

            IncrementLayoutVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "ScrollViewer content changed");
        }
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public IScrollInfo ScrollInfo => Presenter;

    public ScrollContentPresenter Presenter
    {
        get
        {
            ApplyTemplate();
            return presenter ?? throw new InvalidOperationException(
                "ScrollViewer template did not provide the required part 'PART_ScrollContentPresenter'.");
        }
    }

    public ScrollBar HorizontalScrollBar
    {
        get
        {
            ApplyTemplate();
            return horizontalScrollBar ?? throw new InvalidOperationException(
                "ScrollViewer template did not provide the required part 'PART_HorizontalScrollBar'.");
        }
    }

    public ScrollBar VerticalScrollBar
    {
        get
        {
            ApplyTemplate();
            return verticalScrollBar ?? throw new InvalidOperationException(
                "ScrollViewer template did not provide the required part 'PART_VerticalScrollBar'.");
        }
    }

    public bool IsHorizontalScrollBarVisible => HorizontalScrollBar.Visibility == Visibility.Visible;

    public bool IsVerticalScrollBarVisible => VerticalScrollBar.Visibility == Visibility.Visible;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        ConfigureScrollCapabilities();
        ScrollBarState state = ConvergeMeasure(context);
        ApplyScrollBarVisibility(state);
        UpdateScrollBarState();
        return TemplateRoot.Measure(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ApplyTemplate();
        ConfigureScrollCapabilities();
        ScrollBarState state = ConvergeArrange(context);
        ApplyScrollBarVisibility(state);
        UpdateScrollBarState();
        CompleteTemplateRootLayoutPass();
        return context.FinalRect;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        DetachParts();
        if (instance is null)
        {
            return;
        }

        if (instance.Root is null)
        {
            throw new InvalidOperationException("ScrollViewer component template must provide a layout root.");
        }

        ScrollContentPresenter nextPresenter = GetRequiredTemplatePart<ScrollContentPresenter>("PART_ScrollContentPresenter");
        ScrollBar nextHorizontalScrollBar = GetRequiredTemplatePart<ScrollBar>("PART_HorizontalScrollBar");
        ScrollBar nextVerticalScrollBar = GetRequiredTemplatePart<ScrollBar>("PART_VerticalScrollBar");

        presenter = nextPresenter;
        horizontalScrollBar = nextHorizontalScrollBar;
        verticalScrollBar = nextVerticalScrollBar;
        presenter.Content = content;
        presenter.PropertyChanged += OnPresenterPropertyChanged;
        horizontalScrollBar.PropertyChanged += OnScrollBarPropertyChanged;
        verticalScrollBar.PropertyChanged += OnScrollBarPropertyChanged;
        ConfigureScrollCapabilities();
        UpdateScrollBarState();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            GetSourceValue(ComponentTemplateProperty, UiPropertyValueSource.AspectBase) is ComponentTemplate)
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private UIElement TemplateRoot => TemplateChild ?? throw new InvalidOperationException(
        "ScrollViewer component template must provide a layout root.");

    private ScrollBarState ConvergeMeasure(MeasureContext context)
    {
        ScrollBarState state = InitialScrollBarState();
        ScrollBarState seen = state;
        for (int pass = 0; pass < 3; pass++)
        {
            ApplyScrollBarVisibility(state);
            TemplateRoot.Measure(context);
            ScrollBarState next = EvaluateScrollBarState();
            seen = seen.Union(next);
            if (next == state)
            {
                return state;
            }

            state = next;
        }

        ApplyScrollBarVisibility(seen);
        TemplateRoot.Measure(context);
        return seen;
    }

    private ScrollBarState ConvergeArrange(ArrangeContext context)
    {
        ScrollBarState state = InitialScrollBarState();
        ScrollBarState seen = state;
        MeasureContext measureContext = new(context.FinalRect.Size, context.Rounding);
        for (int pass = 0; pass < 3; pass++)
        {
            ApplyScrollBarVisibility(state);
            TemplateRoot.Measure(measureContext);
            UpdateScrollBarState();
            TemplateRoot.Arrange(context);
            ScrollBarState next = EvaluateScrollBarState();
            seen = seen.Union(next);
            if (next == state)
            {
                return state;
            }

            state = next;
        }

        ApplyScrollBarVisibility(seen);
        TemplateRoot.Measure(measureContext);
        UpdateScrollBarState();
        TemplateRoot.Arrange(context);
        return seen;
    }

    private ScrollBarState InitialScrollBarState()
    {
        return new ScrollBarState(
            ShowsScrollBar(HorizontalScrollBarVisibility, horizontalScrollBar?.Visibility == Visibility.Visible),
            ShowsScrollBar(VerticalScrollBarVisibility, verticalScrollBar?.Visibility == Visibility.Visible));
    }

    private ScrollBarState EvaluateScrollBarState()
    {
        ScrollContentPresenter activePresenter = Presenter;
        return new ScrollBarState(
            ShowsScrollBar(HorizontalScrollBarVisibility, activePresenter.ExtentWidth > activePresenter.ViewportWidth),
            ShowsScrollBar(VerticalScrollBarVisibility, activePresenter.ExtentHeight > activePresenter.ViewportHeight));
    }

    private void ApplyScrollBarVisibility(ScrollBarState state)
    {
        if (horizontalScrollBar is not null)
        {
            SetScrollBarVisibility(
                horizontalScrollBar,
                ToVisibility(HorizontalScrollBarVisibility, state.Horizontal));
        }

        if (verticalScrollBar is not null)
        {
            SetScrollBarVisibility(
                verticalScrollBar,
                ToVisibility(VerticalScrollBarVisibility, state.Vertical));
        }
    }

    private static void SetScrollBarVisibility(ScrollBar scrollBar, Visibility visibility)
    {
        if (scrollBar.Visibility == visibility)
        {
            return;
        }

        scrollBar.Visibility = visibility;
        UIRoot? root = scrollBar.Root;
        if (root is null)
        {
            return;
        }

        // Visibility invalidates the hierarchy, but convergence immediately performs that layout.
        for (UIElement? current = scrollBar; current is not null; current = current.VisualParent)
        {
            root.LayoutQueue.RemoveMeasure(current);
            root.LayoutQueue.RemoveArrange(current);
            current.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Arrange);
            if (current is UIRoot)
            {
                break;
            }
        }
    }

    private void ConfigureScrollCapabilities()
    {
        if (presenter is null)
        {
            return;
        }

        presenter.CanHorizontallyScroll = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
        presenter.CanVerticallyScroll = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
    }

    private void CompleteTemplateRootLayoutPass()
    {
        UIElement templateRoot = TemplateRoot;
        UIRoot? root = templateRoot.Root;
        if (root is null)
        {
            return;
        }

        // The owner measures and arranges the template root synchronously during convergence.
        root.LayoutQueue.RemoveMeasure(templateRoot);
        root.LayoutQueue.RemoveArrange(templateRoot);
        templateRoot.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Arrange);
    }

    private void DetachParts()
    {
        if (presenter is not null)
        {
            presenter.PropertyChanged -= OnPresenterPropertyChanged;
            presenter.Content = null;
        }

        if (horizontalScrollBar is not null)
        {
            horizontalScrollBar.PropertyChanged -= OnScrollBarPropertyChanged;
        }

        if (verticalScrollBar is not null)
        {
            verticalScrollBar.PropertyChanged -= OnScrollBarPropertyChanged;
        }

        presenter = null;
        horizontalScrollBar = null;
        verticalScrollBar = null;
    }

    private void OnMouseWheel(UiElementId source, RoutedEventArgs args)
    {
        if (args is not MouseWheelEventArgs wheelArgs ||
            VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
            presenter is null)
        {
            return;
        }

        float oldOffset = presenter.VerticalOffset;
        presenter.SetVerticalOffset(presenter.VerticalOffset - (MathF.Sign(wheelArgs.Delta) * LineScrollAmount));
        args.Handled = oldOffset != presenter.VerticalOffset;
        UpdateScrollBarState();
    }

    private void OnPresenterPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (syncingScrollBars ||
            !ReferenceEquals(sender, presenter) ||
            presenter is null ||
            (!ReferenceEquals(args.Property, ScrollContentPresenter.HorizontalOffsetProperty) &&
             !ReferenceEquals(args.Property, ScrollContentPresenter.VerticalOffsetProperty)))
        {
            return;
        }

        float oldHorizontal = ReferenceEquals(args.Property, ScrollContentPresenter.HorizontalOffsetProperty)
            ? (float)args.OldValue!
            : presenter.HorizontalOffset;
        float oldVertical = ReferenceEquals(args.Property, ScrollContentPresenter.VerticalOffsetProperty)
            ? (float)args.OldValue!
            : presenter.VerticalOffset;
        UpdateScrollBarState();
        RaiseEvent(new ScrollChangedEventArgs(
            ScrollChangedEvent,
            this,
            oldHorizontal,
            oldVertical,
            presenter.HorizontalOffset,
            presenter.VerticalOffset));
    }

    private void UpdateScrollBarState()
    {
        if (presenter is null || horizontalScrollBar is null || verticalScrollBar is null)
        {
            return;
        }

        syncingScrollBars = true;
        try
        {
            horizontalScrollBar.Orientation = Orientation.Horizontal;
            horizontalScrollBar.Minimum = 0;
            horizontalScrollBar.Maximum = MathF.Max(0, presenter.ExtentWidth - presenter.ViewportWidth);
            horizontalScrollBar.ViewportSize = presenter.ViewportWidth;
            horizontalScrollBar.SmallChange = LineScrollAmount;
            horizontalScrollBar.Value = presenter.HorizontalOffset;

            verticalScrollBar.Orientation = Orientation.Vertical;
            verticalScrollBar.Minimum = 0;
            verticalScrollBar.Maximum = MathF.Max(0, presenter.ExtentHeight - presenter.ViewportHeight);
            verticalScrollBar.ViewportSize = presenter.ViewportHeight;
            verticalScrollBar.SmallChange = LineScrollAmount;
            verticalScrollBar.Value = presenter.VerticalOffset;
        }
        finally
        {
            syncingScrollBars = false;
        }
    }

    private void OnScrollBarPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (syncingScrollBars ||
            presenter is null ||
            horizontalScrollBar is null ||
            verticalScrollBar is null ||
            (!ReferenceEquals(sender, horizontalScrollBar) && !ReferenceEquals(sender, verticalScrollBar)) ||
            !ReferenceEquals(args.Property, RangeBase.ValueProperty))
        {
            return;
        }

        presenter.SetHorizontalOffset(horizontalScrollBar.Value);
        presenter.SetVerticalOffset(verticalScrollBar.Value);
        UpdateScrollBarState();
    }

    private static bool ShowsScrollBar(ScrollBarVisibility visibility, bool scrollable)
    {
        return visibility == ScrollBarVisibility.Visible ||
            (visibility == ScrollBarVisibility.Auto && scrollable);
    }

    private static Visibility ToVisibility(ScrollBarVisibility visibility, bool visible)
    {
        return visibility switch
        {
            ScrollBarVisibility.Disabled => Visibility.Collapsed,
            ScrollBarVisibility.Hidden => Visibility.Hidden,
            ScrollBarVisibility.Visible => Visibility.Visible,
            _ => visible ? Visibility.Visible : Visibility.Collapsed
        };
    }

    private readonly record struct ScrollBarState(bool Horizontal, bool Vertical)
    {
        public ScrollBarState Union(ScrollBarState other)
        {
            return new ScrollBarState(Horizontal || other.Horizontal, Vertical || other.Vertical);
        }
    }
}

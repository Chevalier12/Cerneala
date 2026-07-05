using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

public class ScrollViewer : Control
{
    private const float ScrollBarThickness = 12;
    private const float WheelScrollAmount = 48;
    private readonly ScrollContentPresenter presenter;
    private readonly ScrollBar horizontalScrollBar;
    private readonly ScrollBar verticalScrollBar;
    private bool syncingScrollBars;
    private object? content;

    public ScrollViewer()
    {
        presenter = new ScrollContentPresenter();
        horizontalScrollBar = new ScrollBar { Orientation = Orientation.Horizontal };
        verticalScrollBar = new ScrollBar { Orientation = Orientation.Vertical };
        horizontalScrollBar.Visibility = Visibility.Collapsed;
        presenter.PropertyChanged += OnPresenterPropertyChanged;
        horizontalScrollBar.PropertyChanged += OnScrollBarPropertyChanged;
        verticalScrollBar.PropertyChanged += OnScrollBarPropertyChanged;
        AddOwnedChild(presenter);
        AddOwnedChild(horizontalScrollBar);
        AddOwnedChild(verticalScrollBar);
        Handlers.AddHandler(InputEvents.MouseWheelEvent, OnMouseWheel);
    }

    public static readonly UiProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty = UiProperty<ScrollBarVisibility>.Register(
        nameof(HorizontalScrollBarVisibility),
        typeof(ScrollViewer),
        new UiPropertyMetadata<ScrollBarVisibility>(ScrollBarVisibility.Disabled, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty = UiProperty<ScrollBarVisibility>.Register(
        nameof(VerticalScrollBarVisibility),
        typeof(ScrollViewer),
        new UiPropertyMetadata<ScrollBarVisibility>(ScrollBarVisibility.Auto, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public object? Content
    {
        get => content;
        set
        {
            if (ContentControl.ContentEqualityComparer.Equals(content, value))
            {
                content = value;
                presenter.Content = value;
                return;
            }

            content = value;
            presenter.Content = value;
            Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Measure | Cerneala.UI.Invalidation.InvalidationFlags.Render, "ScrollViewer content changed");
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

    public IScrollInfo ScrollInfo => presenter;

    public ScrollContentPresenter Presenter => presenter;

    public ScrollBar HorizontalScrollBar => horizontalScrollBar;

    public ScrollBar VerticalScrollBar => verticalScrollBar;

    public bool IsHorizontalScrollBarVisible => horizontalScrollBar.Visibility == Visibility.Visible;

    public bool IsVerticalScrollBarVisible => verticalScrollBar.Visibility == Visibility.Visible;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        LayoutSize available = context.AvailableSize;
        presenter.CanHorizontallyScroll = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
        presenter.CanVerticallyScroll = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;

        bool needsHorizontal = HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
        bool needsVertical = VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
        for (int pass = 0; pass < 3; pass++)
        {
            bool reserveHorizontal = ReservesSpace(HorizontalScrollBarVisibility) || needsHorizontal;
            bool reserveVertical = ReservesSpace(VerticalScrollBarVisibility) || needsVertical;
            LayoutSize presenterAvailable = new(
                DeflateAvailable(available.Width, reserveVertical),
                DeflateAvailable(available.Height, reserveHorizontal));

            presenter.Measure(new MeasureContext(presenterAvailable, context.Rounding));
            bool nextNeedsHorizontal = ShowsScrollBar(HorizontalScrollBarVisibility, presenter.ExtentWidth > presenter.ViewportWidth);
            bool nextNeedsVertical = ShowsScrollBar(VerticalScrollBarVisibility, presenter.ExtentHeight > presenter.ViewportHeight);
            if (nextNeedsHorizontal == needsHorizontal && nextNeedsVertical == needsVertical)
            {
                break;
            }

            needsHorizontal = nextNeedsHorizontal;
            needsVertical = nextNeedsVertical;
        }

        UpdateScrollBarState();
        horizontalScrollBar.Visibility = ToVisibility(HorizontalScrollBarVisibility, needsHorizontal);
        verticalScrollBar.Visibility = ToVisibility(VerticalScrollBarVisibility, needsVertical);
        horizontalScrollBar.Measure(new MeasureContext(new LayoutSize(presenter.ViewportWidth, ScrollBarThickness), context.Rounding));
        verticalScrollBar.Measure(new MeasureContext(new LayoutSize(ScrollBarThickness, presenter.ViewportHeight), context.Rounding));

        bool finalReserveHorizontal = ReservesSpace(HorizontalScrollBarVisibility) || needsHorizontal;
        bool finalReserveVertical = ReservesSpace(VerticalScrollBarVisibility) || needsVertical;
        float width = float.IsPositiveInfinity(available.Width)
            ? presenter.DesiredSize.Width + (finalReserveVertical ? ScrollBarThickness : 0)
            : available.Width;
        float height = float.IsPositiveInfinity(available.Height)
            ? presenter.DesiredSize.Height + (finalReserveHorizontal ? ScrollBarThickness : 0)
            : available.Height;
        return new LayoutSize(MathF.Max(0, width), MathF.Max(0, height));
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        bool verticalSpace = verticalScrollBar.Visibility is Visibility.Visible or Visibility.Hidden;
        bool horizontalSpace = horizontalScrollBar.Visibility is Visibility.Visible or Visibility.Hidden;
        LayoutRect presenterRect = new(
            context.FinalRect.X,
            context.FinalRect.Y,
            MathF.Max(0, context.FinalRect.Width - (verticalSpace ? ScrollBarThickness : 0)),
            MathF.Max(0, context.FinalRect.Height - (horizontalSpace ? ScrollBarThickness : 0)));
        presenter.Arrange(new ArrangeContext(presenterRect, context.Rounding));
        UpdateScrollBarState();

        horizontalScrollBar.Arrange(new ArrangeContext(
            new LayoutRect(
                presenterRect.X,
                presenterRect.Y + presenterRect.Height,
                presenterRect.Width,
                horizontalSpace ? ScrollBarThickness : 0),
            context.Rounding));
        verticalScrollBar.Arrange(new ArrangeContext(
            new LayoutRect(
                presenterRect.X + presenterRect.Width,
                presenterRect.Y,
                verticalSpace ? ScrollBarThickness : 0,
                presenterRect.Height),
            context.Rounding));
        return context.FinalRect;
    }

    private void OnMouseWheel(UiElementId source, RoutedEventArgs args)
    {
        if (args is not MouseWheelEventArgs wheelArgs || VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
        {
            return;
        }

        float oldOffset = presenter.VerticalOffset;
        presenter.SetVerticalOffset(presenter.VerticalOffset - (MathF.Sign(wheelArgs.Delta) * WheelScrollAmount));
        args.Handled = oldOffset != presenter.VerticalOffset;
        UpdateScrollBarState();
    }

    private void OnPresenterPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (syncingScrollBars ||
            (!ReferenceEquals(args.Property, ScrollContentPresenter.HorizontalOffsetProperty) &&
             !ReferenceEquals(args.Property, ScrollContentPresenter.VerticalOffsetProperty)))
        {
            return;
        }

        UpdateScrollBarState();
    }

    private void UpdateScrollBarState()
    {
        syncingScrollBars = true;
        try
        {
            horizontalScrollBar.Minimum = 0;
            horizontalScrollBar.Maximum = MathF.Max(0, presenter.ExtentWidth - presenter.ViewportWidth);
            horizontalScrollBar.ViewportSize = presenter.ViewportWidth;
            horizontalScrollBar.Value = presenter.HorizontalOffset;

            verticalScrollBar.Minimum = 0;
            verticalScrollBar.Maximum = MathF.Max(0, presenter.ExtentHeight - presenter.ViewportHeight);
            verticalScrollBar.ViewportSize = presenter.ViewportHeight;
            verticalScrollBar.Value = presenter.VerticalOffset;
        }
        finally
        {
            syncingScrollBars = false;
        }
    }

    private void OnScrollBarPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (syncingScrollBars || !ReferenceEquals(args.Property, RangeBase.ValueProperty))
        {
            return;
        }

        presenter.SetHorizontalOffset(horizontalScrollBar.Value);
        presenter.SetVerticalOffset(verticalScrollBar.Value);
        UpdateScrollBarState();
    }

    private void AddOwnedChild(UIElement child)
    {
        LogicalChildren.Add(child);
        VisualChildren.Add(child);
    }

    private static float DeflateAvailable(float size, bool deflate)
    {
        if (float.IsPositiveInfinity(size))
        {
            return size;
        }

        return MathF.Max(0, size - (deflate ? ScrollBarThickness : 0));
    }

    private static bool ReservesSpace(ScrollBarVisibility visibility)
    {
        return visibility is ScrollBarVisibility.Hidden or ScrollBarVisibility.Visible;
    }

    private static bool ShowsScrollBar(ScrollBarVisibility visibility, bool scrollable)
    {
        return visibility == ScrollBarVisibility.Visible || (visibility == ScrollBarVisibility.Auto && scrollable);
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
}

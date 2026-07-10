using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Rendering;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public class ScrollBar : RangeBase
{
    public static readonly RoutedEvent ScrollEvent = RoutedEventRegistry.Register(nameof(Scroll), typeof(ScrollBar), RoutingStrategy.Bubble, typeof(ScrollEventArgs));

    public event EventHandler<ScrollEventArgs> Scroll { add => AddTypedHandler(ScrollEvent, value); remove => RemoveTypedHandler(ScrollEvent, value); }
    private readonly Track track;
    private bool ownsTrack;
    private bool syncingTrack;

    public ScrollBar()
    {
        track = new Track();
        track.ValueChanged += OnTrackValueChanged;
        AddTrack();
        Background = new DrawColor(235, 235, 235);
        BorderColor = new DrawColor(130, 130, 130);
        BorderThickness = new Thickness(1);
        ViewportSize = 0;
        SyncTrack();
    }

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(ScrollBar),
        new UiPropertyMetadata<Orientation>(Orientation.Vertical, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<float> ViewportSizeProperty = UiProperty<float>.Register(
        nameof(ViewportSize),
        typeof(ScrollBar),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender, validateValue: value => value >= 0 && float.IsFinite(value)));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public float ViewportSize
    {
        get => GetValue(ViewportSizeProperty);
        set => SetValue(ViewportSizeProperty, value);
    }

    public Track Track => track;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        SyncTrack();
        track.Measure(new MeasureContext(context.AvailableSize, context.Rounding));
        return Orientation == Orientation.Horizontal
            ? new LayoutSize(MathF.Max(32, track.DesiredSize.Width), 12)
            : new LayoutSize(12, MathF.Max(32, track.DesiredSize.Height));
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        SyncTrack();
        track.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is not null)
        {
            return;
        }

        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, Background);
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        bool templateChanged = ReferenceEquals(args.Property, ComponentTemplateProperty);
        base.OnPropertyChanged(args);
        if (!syncingTrack &&
            (ReferenceEquals(args.Property, MinimumProperty) ||
            ReferenceEquals(args.Property, MaximumProperty) ||
            ReferenceEquals(args.Property, ValueProperty) ||
            ReferenceEquals(args.Property, SmallChangeProperty) ||
            ReferenceEquals(args.Property, LargeChangeProperty) ||
            ReferenceEquals(args.Property, ViewportSizeProperty) ||
            ReferenceEquals(args.Property, OrientationProperty)))
        {
            SyncTrack();
        }

        if (templateChanged)
        {
            if (ComponentTemplate is null)
            {
                AddTrack();
            }
            else
            {
                RemoveTrack();
            }
        }
    }

    private void SyncTrack()
    {
        syncingTrack = true;
        try
        {
            track.Minimum = Minimum;
            track.Maximum = Maximum;
            track.Value = Value;
            track.SmallChange = SmallChange;
            track.LargeChange = LargeChange;
            track.ViewportSize = ViewportSize;
            track.Orientation = Orientation;
        }
        finally
        {
            syncingTrack = false;
        }
    }

    private void OnTrackValueChanged(object? sender, EventArgs args)
    {
        if (syncingTrack)
        {
            return;
        }

        Value = track.Value;
        RaiseEvent(new ScrollEventArgs(ScrollEvent, this, ScrollEventType.ThumbTrack, Value));
    }

    private void AddTrack()
    {
        if (ownsTrack)
        {
            return;
        }

        LogicalChildren.Add(track);
        VisualChildren.Add(track);
        ownsTrack = true;
    }

    private void RemoveTrack()
    {
        if (!ownsTrack)
        {
            return;
        }

        VisualChildren.Remove(track);
        LogicalChildren.Remove(track);
        ownsTrack = false;
    }
}

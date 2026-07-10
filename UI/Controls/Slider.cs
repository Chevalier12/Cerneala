using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

public class Slider : RangeBase
{
    private readonly Track track;
    private bool ownsTrack;
    private bool syncingTrack;

    public Slider()
    {
        track = new Track();
        track.ValueChanged += OnTrackValueChanged;
        AddTrack();
    }

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(Slider),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
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
        return track.DesiredSize;
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

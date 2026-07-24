using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_Track", typeof(Track))]
public class Slider : RangeBase
{
    private Track? track;
    private bool syncingTrack;

    public Slider()
    {
        SetValue(ComponentTemplateProperty, SliderTemplates.Default, UiPropertyValueSource.AspectBase);
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

    public Track Track
    {
        get
        {
            ApplyTemplate();
            return track ?? throw new InvalidOperationException("Slider template did not provide the required part 'PART_Track'.");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        SyncTrack();
        return base.MeasureCore(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        SyncTrack();
        return base.ArrangeCore(context);
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        if (track is not null)
        {
            track.ValueChanged -= OnTrackValueChanged;
        }

        track = null;
        if (instance is null)
        {
            return;
        }

        track = GetRequiredTemplatePart<Track>("PART_Track");
        track.MoveToPointOnClick = true;
        track.ValueChanged += OnTrackValueChanged;
        SyncTrack();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
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

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            GetSourceValue(ComponentTemplateProperty, UiPropertyValueSource.AspectBase) is ComponentTemplate)
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private void SyncTrack()
    {
        if (track is null)
        {
            return;
        }

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
        if (syncingTrack || !ReferenceEquals(sender, track))
        {
            return;
        }

        Value = track!.Value;
    }
}

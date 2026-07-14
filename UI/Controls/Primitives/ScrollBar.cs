using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls.Primitives;

[TemplatePart("PART_Track", typeof(Track))]
[TemplatePart("PART_DecreaseButton", typeof(RepeatButton))]
[TemplatePart("PART_IncreaseButton", typeof(RepeatButton))]
public class ScrollBar : RangeBase
{
    public static readonly RoutedEvent ScrollEvent = RoutedEventRegistry.Register(nameof(Scroll), typeof(ScrollBar), RoutingStrategy.Bubble, typeof(ScrollEventArgs));

    public event EventHandler<ScrollEventArgs> Scroll { add => AddTypedHandler(ScrollEvent, value); remove => RemoveTypedHandler(ScrollEvent, value); }
    private Track? track;
    private RepeatButton? decreaseButton;
    private RepeatButton? increaseButton;
    private bool syncingTrack;

    public ScrollBar()
    {
        Background = new Cerneala.UI.Media.SolidColorBrush(new Color(235, 235, 235));
        BorderBrush = new Cerneala.UI.Media.SolidColorBrush(new Color(130, 130, 130));
        BorderThickness = new Thickness(1);
        ViewportSize = 0;
        SetValue(ComponentTemplateProperty, ScrollBarTemplates.Default, UiPropertyValueSource.AspectBase);
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

    public Track Track
    {
        get
        {
            ApplyTemplate();
            return track ?? throw new InvalidOperationException("ScrollBar template did not provide the required part 'PART_Track'.");
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
            track.ValueChangedWithReason -= OnTrackValueChanged;
        }

        if (decreaseButton is not null)
        {
            decreaseButton.Click -= OnDecreaseClick;
        }

        if (increaseButton is not null)
        {
            increaseButton.Click -= OnIncreaseClick;
        }

        track = null;
        decreaseButton = null;
        increaseButton = null;
        if (instance is null)
        {
            return;
        }

        Track nextTrack = GetRequiredTemplatePart<Track>("PART_Track");
        RepeatButton? nextDecreaseButton = GetOptionalTemplatePart<RepeatButton>("PART_DecreaseButton");
        RepeatButton? nextIncreaseButton = GetOptionalTemplatePart<RepeatButton>("PART_IncreaseButton");

        track = nextTrack;
        decreaseButton = nextDecreaseButton;
        increaseButton = nextIncreaseButton;
        track.ValueChangedWithReason += OnTrackValueChanged;
        if (decreaseButton is not null)
        {
            decreaseButton.Click += OnDecreaseClick;
        }

        if (increaseButton is not null)
        {
            increaseButton.Click += OnIncreaseClick;
        }

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
            ReferenceEquals(args.Property, ViewportSizeProperty) ||
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
            track.ViewportSize = ViewportSize;
            track.Orientation = Orientation;
        }
        finally
        {
            syncingTrack = false;
        }
    }

    private void OnTrackValueChanged(object? sender, TrackValueChangedEventArgs args)
    {
        if (syncingTrack || !ReferenceEquals(sender, track))
        {
            return;
        }

        Value = args.NewValue;
        if (TryMapScrollEventType(args.Reason, out ScrollEventType scrollEventType))
        {
            RaiseEvent(new ScrollEventArgs(ScrollEvent, this, scrollEventType, Value));
        }
    }

    private void OnDecreaseClick(UiElementId source, RoutedEventArgs args)
    {
        track?.DecreaseSmall();
        args.Handled = true;
    }

    private void OnIncreaseClick(UiElementId source, RoutedEventArgs args)
    {
        track?.IncreaseSmall();
        args.Handled = true;
    }

    private static bool TryMapScrollEventType(TrackValueChangeReason reason, out ScrollEventType eventType)
    {
        eventType = reason switch
        {
            TrackValueChangeReason.SmallDecrement => ScrollEventType.SmallDecrement,
            TrackValueChangeReason.SmallIncrement => ScrollEventType.SmallIncrement,
            TrackValueChangeReason.LargeDecrement => ScrollEventType.LargeDecrement,
            TrackValueChangeReason.LargeIncrement => ScrollEventType.LargeIncrement,
            TrackValueChangeReason.ThumbTrack => ScrollEventType.ThumbTrack,
            _ => default
        };

        return reason != TrackValueChangeReason.Programmatic;
    }
}

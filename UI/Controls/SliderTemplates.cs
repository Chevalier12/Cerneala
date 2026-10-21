using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

internal static class SliderTemplates
{
    // As WPF's default Slider template: the Track between two TickBars, which TickPlacement shows or collapses. The
    // TickBars take the slider's range, ticks and direction, and draw with its Foreground. As WPF's TrackBackground, a
    // track bar with the Track's former colors lies under the Track, and the selection range lies on the bar; the Track
    // itself draws no background and no border, so its transparent repeat buttons and its thumb stay on top.
    public static readonly ComponentTemplate<Slider> Default = new("Slider.Default", context => Create(context, withTrackBar: true));

    // The template of the ColorPicker's color ramps: the Default template without the track bar and the selection range,
    // so the Track draws the ramp's gradient itself and the checkerboard behind the alpha ramp stays visible through it.
    public static readonly ComponentTemplate<Slider> ColorRamp = new("Slider.ColorRamp", context => Create(context, withTrackBar: false));

    private static SliderLayoutPanel Create(ComponentTemplateContext<Slider> context, bool withTrackBar)
    {
        TickBar topLeftTickBar = CreateTickBar(context);
        Track track = new();
        TickBar bottomRightTickBar = CreateTickBar(context);
        Border? trackBar = null;
        Border? selectionRange = null;
        if (withTrackBar)
        {
            trackBar = new Border
            {
                Background = new SolidColorBrush(new Color(225, 225, 225)),
                BorderBrush = new SolidColorBrush(new Color(120, 120, 120)),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false
            };

            // As WPF's PART_SelectionRange, filled with the Windows highlight color, (0, 120, 215).
            selectionRange = new Border
            {
                Background = new SolidColorBrush(new Color(0, 120, 215)),
                IsHitTestVisible = false
            };

            // Below aspects and local values, so an aspect or a local value on the Track still gives it a background.
            track.SetValue(Control.BackgroundProperty, null, UiPropertyValueSource.TemplateBinding);
            track.SetValue(Control.BorderBrushProperty, null, UiPropertyValueSource.TemplateBinding);
        }

        SliderLayoutPanel panel = new(topLeftTickBar, trackBar, selectionRange, track, bottomRightTickBar);
        context.RequirePart("PART_Track", track);
        if (selectionRange is not null)
        {
            context.RequirePart("PART_SelectionRange", selectionRange);
        }

        context.Bind(Slider.OrientationProperty, panel, SliderLayoutPanel.OrientationProperty);
        context.Bind(Slider.TickPlacementProperty, panel, SliderLayoutPanel.TickPlacementProperty);
        context.Bind(RangeBase.MinimumProperty, panel, SliderLayoutPanel.MinimumProperty);
        context.Bind(RangeBase.MaximumProperty, panel, SliderLayoutPanel.MaximumProperty);
        context.Bind(Slider.SelectionStartProperty, panel, SliderLayoutPanel.SelectionStartProperty);
        context.Bind(Slider.SelectionEndProperty, panel, SliderLayoutPanel.SelectionEndProperty);
        context.Bind(Slider.IsDirectionReversedProperty, panel, SliderLayoutPanel.IsDirectionReversedProperty);
        return panel;
    }

    private static TickBar CreateTickBar(ComponentTemplateContext<Slider> context)
    {
        TickBar tickBar = new();
        context.Bind(RangeBase.MinimumProperty, tickBar, TickBar.MinimumProperty);
        context.Bind(RangeBase.MaximumProperty, tickBar, TickBar.MaximumProperty);
        context.Bind(Slider.TickFrequencyProperty, tickBar, TickBar.TickFrequencyProperty);
        context.Bind(Slider.TicksProperty, tickBar, TickBar.TicksProperty);
        context.Bind(Slider.IsDirectionReversedProperty, tickBar, TickBar.IsDirectionReversedProperty);
        context.Bind(Control.ForegroundProperty, tickBar, TickBar.FillProperty);
        return tickBar;
    }
}

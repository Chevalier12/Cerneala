using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls.Primitives;

internal static class TrackTemplates
{
    public static readonly ComponentTemplate<Track> Default = new("Track.Default", context =>
    {
        RepeatButton decreaseButton = CreateTrackRepeatButton();
        RepeatButton increaseButton = CreateTrackRepeatButton();
        Thumb thumb = new();
        TrackLayoutPanel panel = new();
        Border root = new() { Child = panel };
        panel.VisualChildren.Add(decreaseButton);
        panel.VisualChildren.Add(increaseButton);
        panel.VisualChildren.Add(thumb);

        context.RequirePart("PART_DecreaseRepeatButton", decreaseButton);
        context.RequirePart("PART_IncreaseRepeatButton", increaseButton);
        context.RequirePart("PART_Thumb", thumb);
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, UiPropertyValueSource.Local);
        context.Bind(Track.OrientationProperty, panel, TrackLayoutPanel.OrientationProperty);

        return root;
    });

    // As the transparent RepeatButtons of WPF's Slider and ScrollBar track templates: they take presses on the track
    // beside the thumb behind it, draw nothing in any state and take no focus. The local values replace the default
    // aspect's button background, border and padding, so they neither draw nor change the track's size. Hit testing
    // uses the arranged bounds, so no background brush is needed to make them hit-testable, and the cursor stays the
    // one of the track's ancestors.
    private static RepeatButton CreateTrackRepeatButton()
    {
        return new RepeatButton
        {
            Background = null,
            BorderBrush = null,
            BorderThickness = Thickness.Zero,
            Padding = Thickness.Zero,
            Focusable = false,
            IsTabStop = false,
            Cursor = null
        };
    }
}

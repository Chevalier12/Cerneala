using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;

namespace Cerneala.UI.Controls.Primitives;

internal static class TrackTemplates
{
    public static readonly ComponentTemplate<Track> Default = new("Track.Default", context =>
    {
        Thumb thumb = new();
        TrackLayoutPanel panel = new();
        Border root = new() { Child = panel };
        panel.VisualChildren.Add(thumb);

        context.RequirePart("PART_Thumb", thumb);
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, UiPropertyValueSource.Local);
        context.Bind(Track.OrientationProperty, panel, TrackLayoutPanel.OrientationProperty);

        return root;
    });
}

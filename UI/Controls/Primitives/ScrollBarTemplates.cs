using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Primitives;

internal static class ScrollBarTemplates
{
    public static readonly ComponentTemplate<ScrollBar> Default = new("ScrollBar.Default", context =>
    {
        RepeatButton decreaseButton = CreateDirectionButton();
        Track track = new();
        RepeatButton increaseButton = CreateDirectionButton();
        ScrollBarLayoutPanel panel = new(decreaseButton, track, increaseButton);
        Border root = new() { Child = panel };

        context.RequirePart("PART_DecreaseButton", decreaseButton);
        context.RequirePart("PART_Track", track);
        context.RequirePart("PART_IncreaseButton", increaseButton);
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, UiPropertyValueSource.Local);
        context.Bind(ScrollBar.OrientationProperty, panel, ScrollBarLayoutPanel.OrientationProperty);

        return root;
    });

    // As the RepeatButtons of the WPF default ScrollBar template, the arrows take no focus: Tab does not stop on them and a
    // click on one scrolls without moving the keyboard focus from the element that has it (#700).
    private static RepeatButton CreateDirectionButton()
    {
        return new RepeatButton
        {
            Focusable = false,
            IsTabStop = false,
            Background = new SolidColorBrush(new Color(245, 245, 245)),
            BorderBrush = new SolidColorBrush(new Color(130, 130, 130)),
            BorderThickness = new Thickness(1),
            FontSize = 9,
            Padding = new Thickness(1)
        };
    }
}

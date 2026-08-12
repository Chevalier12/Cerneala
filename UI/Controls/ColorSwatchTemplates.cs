using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

internal static class ColorSwatchTemplates
{
    public static readonly ComponentTemplate<ColorSwatch> Default = new("ColorSwatch.Default", context =>
    {
        Button button = new()
        {
            Padding = Thickness.Zero,
            Cursor = Cursor.Hand
        };
        Overlay overlay = new()
        {
            PlacementTarget = button,
            Placement = OverlayPlacement.Auto,
            IsLightDismissEnabled = true,
            MatchTargetWidth = false
        };
        Grid root = new();
        Add(root, button);
        Add(root, overlay);

        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.LocalAspectBase;
        context.Bind(Control.BorderBrushProperty, button, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, button, Control.BorderThicknessProperty, OwnerBindingSource);
        context.RequirePart("PART_SwatchButton", button);
        context.RequirePart("PART_PickerOverlay", overlay);
        return root;
    });

    private static void Add(Grid panel, UIElement child)
    {
        panel.LogicalChildren.Add(child);
        panel.VisualChildren.Add(child);
    }
}

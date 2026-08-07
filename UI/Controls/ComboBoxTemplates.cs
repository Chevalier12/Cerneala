using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.UI.Controls;

internal static class ComboBoxTemplates
{
    public static readonly ComponentTemplate<ComboBox> Default = new("ComboBox.Default", context =>
    {
        ContentPresenter selectionPresenter = new();
        TextBox editableTextBox = new()
        {
            Visibility = Visibility.Collapsed
        };
        DirectionPath dropDownGlyph = DirectionGlyphs.Create(DirectionGlyphKind.Down);
        dropDownGlyph.Width = 10;
        dropDownGlyph.Height = 10;
        dropDownGlyph.IsHitTestVisible = false;
        ToggleButton toggle = new()
        {
            Content = dropDownGlyph,
            Padding = new Thickness(8, 2, 8, 2)
        };
        dropDownGlyph.Fill = toggle.Foreground;
        ItemsPresenter itemsPresenter = new();
        ScrollViewer scrollViewer = new()
        {
            Content = itemsPresenter,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Border dropDownBorder = new()
        {
            BorderThickness = new Thickness(1),
            Child = scrollViewer
        };
        context.Bind(Control.BackgroundProperty, dropDownBorder, Control.BackgroundProperty);
        context.Bind(Control.BorderBrushProperty, dropDownBorder, Control.BorderBrushProperty);
        Overlay overlay = new()
        {
            Content = dropDownBorder,
            PlacementTarget = context.Owner,
            Placement = OverlayPlacement.Auto,
            IsLightDismissEnabled = true,
            MatchTargetWidth = true
        };

        Grid root = new();
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(selectionPresenter, 0);
        Grid.SetColumn(editableTextBox, 0);
        Grid.SetColumn(toggle, 1);
        Grid.SetColumn(overlay, 0);
        Grid.SetColumnSpan(overlay, 2);
        root.VisualChildren.Add(selectionPresenter);
        root.VisualChildren.Add(editableTextBox);
        root.VisualChildren.Add(toggle);
        root.VisualChildren.Add(overlay);

        context.RequirePart("PART_SelectionPresenter", selectionPresenter);
        context.RequirePart("PART_EditableTextBox", editableTextBox);
        context.RequirePart("PART_DropDownToggle", toggle);
        context.RequirePart("PART_DropDownOverlay", overlay);
        context.RequirePart("PART_ItemsPresenter", itemsPresenter);
        return root;
    });
}

using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;
using Shape = Cerneala.UI.Controls.Shapes.Shape;

namespace Cerneala.UI.Controls;

internal static class ComboBoxTemplates
{
    public static readonly ComponentTemplate<ComboBox> Default = new("ComboBox.Default", context =>
    {
        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.LocalAspectBase;
        ContentPresenter selectionPresenter = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox editableTextBox = new()
        {
            CenterSingleLineContentVertically = true,
            IsTabStop = false,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed
        };
        DirectionPath dropDownGlyph = DirectionGlyphs.Create(DirectionGlyphKind.Down);
        dropDownGlyph.Width = 10;
        dropDownGlyph.Height = 10;
        dropDownGlyph.IsHitTestVisible = false;
        ToggleButton toggle = new()
        {
            Content = dropDownGlyph,
            IsTabStop = false,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 2, 8, 2)
        };
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
        context.Bind(Control.BackgroundProperty, dropDownBorder, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, dropDownBorder, Control.BorderBrushProperty, OwnerBindingSource);
        Overlay overlay = new()
        {
            Content = dropDownBorder,
            PlacementTarget = context.Owner,
            Placement = OverlayPlacement.Auto,
            IsLightDismissEnabled = true,
            MatchTargetWidth = true
        };

        Grid fieldGrid = new();
        fieldGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        fieldGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(selectionPresenter, 0);
        Grid.SetColumn(editableTextBox, 0);
        Grid.SetColumn(toggle, 1);
        fieldGrid.VisualChildren.Add(selectionPresenter);
        fieldGrid.VisualChildren.Add(editableTextBox);
        fieldGrid.VisualChildren.Add(toggle);
        Border fieldBorder = new()
        {
            Child = fieldGrid
        };
        context.Bind(Control.BackgroundProperty, fieldBorder, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, fieldBorder, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, fieldBorder, Control.BorderThicknessProperty, OwnerBindingSource);
        context.Bind(Control.BackgroundProperty, editableTextBox, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.ForegroundProperty, editableTextBox, Control.ForegroundProperty, OwnerBindingSource);
        context.Bind(
            (UiProperty)Control.ForegroundProperty,
            editableTextBox,
            (UiProperty)TextBox.CaretBrushProperty,
            OwnerBindingSource);
        context.Bind(Control.BackgroundProperty, toggle, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.ForegroundProperty, toggle, Control.ForegroundProperty, OwnerBindingSource);
        context.Bind(Control.ForegroundProperty, selectionPresenter, Control.ForegroundProperty, OwnerBindingSource);
        context.Bind(
            Control.ForegroundProperty,
            dropDownGlyph,
            Shape.FillProperty,
            OwnerBindingSource);

        Grid root = new();
        Grid.SetColumn(fieldBorder, 0);
        Grid.SetColumn(overlay, 0);
        root.VisualChildren.Add(fieldBorder);
        root.VisualChildren.Add(overlay);

        context.RequirePart("PART_SelectionPresenter", selectionPresenter);
        context.RequirePart("PART_EditableTextBox", editableTextBox);
        context.RequirePart("PART_DropDownToggle", toggle);
        context.RequirePart("PART_DropDownOverlay", overlay);
        context.RequirePart("PART_ItemsPresenter", itemsPresenter);
        return root;
    });
}

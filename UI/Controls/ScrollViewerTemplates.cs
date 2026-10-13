using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls;

internal static class ScrollViewerTemplates
{
    public static readonly ComponentTemplate<ScrollViewer> Default = new("ScrollViewer.Default", context =>
    {
        ScrollContentPresenter presenter = new();
        ScrollBar horizontalScrollBar = new()
        {
            Orientation = Orientation.Horizontal,
            Visibility = Visibility.Collapsed
        };
        ScrollBar verticalScrollBar = new()
        {
            Orientation = Orientation.Vertical,
            Visibility = Visibility.Collapsed
        };
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        Grid.SetRow(presenter, 0);
        Grid.SetColumn(presenter, 0);
        Grid.SetRow(verticalScrollBar, 0);
        Grid.SetColumn(verticalScrollBar, 1);
        Grid.SetRow(horizontalScrollBar, 1);
        Grid.SetColumn(horizontalScrollBar, 0);
        grid.VisualChildren.Add(presenter);
        grid.VisualChildren.Add(verticalScrollBar);
        grid.VisualChildren.Add(horizontalScrollBar);
        Border root = new() { Child = grid };

        context.RequirePart("PART_ScrollContentPresenter", presenter);
        context.RequirePart("PART_HorizontalScrollBar", horizontalScrollBar);
        context.RequirePart("PART_VerticalScrollBar", verticalScrollBar);
        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.TemplateOwnerBinding;
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, OwnerBindingSource);
        return root;
    });
}

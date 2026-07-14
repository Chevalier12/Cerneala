using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
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
        Grid root = new();
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        Grid.SetRow(presenter, 0);
        Grid.SetColumn(presenter, 0);
        Grid.SetRow(verticalScrollBar, 0);
        Grid.SetColumn(verticalScrollBar, 1);
        Grid.SetRow(horizontalScrollBar, 1);
        Grid.SetColumn(horizontalScrollBar, 0);
        root.VisualChildren.Add(presenter);
        root.VisualChildren.Add(verticalScrollBar);
        root.VisualChildren.Add(horizontalScrollBar);

        context.RequirePart("PART_ScrollContentPresenter", presenter);
        context.RequirePart("PART_HorizontalScrollBar", horizontalScrollBar);
        context.RequirePart("PART_VerticalScrollBar", verticalScrollBar);
        return root;
    });
}

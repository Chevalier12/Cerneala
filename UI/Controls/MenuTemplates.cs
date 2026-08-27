using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using DirectionPath = Cerneala.UI.Controls.Shapes.Path;
using Shape = Cerneala.UI.Controls.Shapes.Shape;

namespace Cerneala.UI.Controls;

internal static class MenuTemplates
{
    public static readonly ComponentTemplate<Menu> Menu = new("Menu.Default", context =>
    {
        ItemsPresenter presenter = new();
        context.RequirePart("PART_ItemsPresenter", presenter);
        return presenter;
    });

    public static readonly ComponentTemplate<MenuBar> MenuBar = new("MenuBar.Default", context =>
    {
        ItemsPresenter presenter = new();
        context.RequirePart("PART_ItemsPresenter", presenter);
        return presenter;
    });

    public static readonly ComponentTemplate<MenuItem> MenuItem = new("MenuItem.Default", context =>
    {
        const UiPropertyValueSource ownerBindingSource = UiPropertyValueSource.TemplateOwnerBinding;
        ContentPresenter headerPresenter = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        DirectionPath submenuIndicator = DirectionGlyphs.Create(DirectionGlyphKind.Right);
        submenuIndicator.Width = 9;
        submenuIndicator.Height = 9;
        submenuIndicator.IsHitTestVisible = false;

        Grid headerGrid = new();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(headerPresenter, 0);
        Grid.SetColumn(submenuIndicator, 1);
        headerGrid.VisualChildren.Add(headerPresenter);
        headerGrid.VisualChildren.Add(submenuIndicator);

        Border headerBorder = new()
        {
            Child = headerGrid
        };
        context.Bind(Control.BackgroundProperty, headerBorder, Control.BackgroundProperty, ownerBindingSource);
        context.Bind(Control.BorderBrushProperty, headerBorder, Control.BorderBrushProperty, ownerBindingSource);
        context.Bind(Control.BorderThicknessProperty, headerBorder, Control.BorderThicknessProperty, ownerBindingSource);
        context.Bind(Control.PaddingProperty, headerBorder, Control.PaddingProperty, ownerBindingSource);
        context.Bind(Control.ForegroundProperty, headerPresenter, Control.ForegroundProperty, ownerBindingSource);
        context.Bind(Control.ForegroundProperty, submenuIndicator, Shape.FillProperty, ownerBindingSource);

        ItemsPresenter itemsPresenter = new();
        ScrollViewer scrollViewer = new()
        {
            Content = itemsPresenter,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Border submenuBorder = new()
        {
            BorderThickness = new Thickness(1),
            Child = scrollViewer
        };
        context.Bind(Control.BackgroundProperty, submenuBorder, Control.BackgroundProperty, ownerBindingSource);
        context.Bind(Control.BorderBrushProperty, submenuBorder, Control.BorderBrushProperty, ownerBindingSource);
        Overlay overlay = new()
        {
            Content = submenuBorder,
            PlacementTarget = context.Owner,
            Placement = OverlayPlacement.AutoHorizontal,
            IsLightDismissEnabled = true
        };

        Grid root = new();
        root.VisualChildren.Add(headerBorder);
        root.VisualChildren.Add(overlay);
        context.RequirePart("PART_HeaderPresenter", headerPresenter);
        context.RequirePart("PART_SubmenuIndicator", submenuIndicator);
        context.RequirePart("PART_SubmenuOverlay", overlay);
        context.RequirePart("PART_ItemsPresenter", itemsPresenter);
        return root;
    });
}

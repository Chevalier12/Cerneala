using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

internal static class ListBoxTemplates
{
    // As the WPF ListBox template: a Border with the owner's Background, BorderBrush, BorderThickness, and Padding around a
    // ScrollViewer around the items presenter. The rows stay inside BorderThickness and Padding, and Background fills the
    // arranged bounds.
    public static readonly ComponentTemplate<ListBox> Default = new("ListBox.Default", context =>
    {
        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.TemplateOwnerBinding;

        ItemsPresenter itemsPresenter = new();
        ScrollViewer scrollViewer = new()
        {
            Content = itemsPresenter,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Border border = new() { Child = scrollViewer };
        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, border, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, border, Control.BorderThicknessProperty, OwnerBindingSource);
        context.Bind(Control.PaddingProperty, border, Control.PaddingProperty, OwnerBindingSource);

        context.RequirePart("PART_ScrollViewer", scrollViewer);
        context.RequirePart("PART_ItemsPresenter", itemsPresenter);
        return border;
    });
}

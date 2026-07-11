using Cerneala.UI.Core;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonTemplates
{
    public static readonly ComponentTemplate<Button> Modern = new("Button.Modern", context =>
    {
        ContentPresenter presenter = new();
        Border border = new() { Child = presenter };

        presenter.ResourceProvider = context.Owner.ResourceProvider;
        presenter.FontResourceId = context.Owner.FontResourceId;

        context.RegisterSlot(ButtonSlots.Root, border);
        context.RegisterSlot(ButtonSlots.Content, presenter);
        context.RequirePart("PART_Content", presenter);
        context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderBrushProperty, border, Control.BorderBrushProperty, UiPropertyValueSource.Local);
        context.Bind(Control.BorderThicknessProperty, border, Control.BorderThicknessProperty, UiPropertyValueSource.Local);
        context.Bind(Control.PaddingProperty, border, Control.PaddingProperty, UiPropertyValueSource.Local);
        context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
        context.Bind(Control.ForegroundProperty, presenter, Control.ForegroundProperty);

        return border;
    });
}

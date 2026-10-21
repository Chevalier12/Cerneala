using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

// The default templates of TextBox and PasswordBox, as WPF's: a Border with the owner's background and border, and inside
// it the ScrollViewer part PART_ContentHost, inset by the owner's Padding. The owner puts its text view into that
// ScrollViewer (TextInputCore.AttachContentHost).
internal static class TextInputTemplates
{
    public const string ContentHostPartName = "PART_ContentHost";

    public static readonly ComponentTemplate<TextBox> TextBox = new("TextBox.Default", CreateRoot);

    public static readonly ComponentTemplate<PasswordBox> PasswordBox = new("PasswordBox.Default", CreateRoot);

    private static Border CreateRoot(ComponentTemplateContext context)
    {
        ScrollViewer contentHost = new();
        Border root = new() { Child = contentHost };

        context.RequirePart(ContentHostPartName, contentHost);
        const UiPropertyValueSource OwnerBindingSource = UiPropertyValueSource.TemplateOwnerBinding;
        context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, OwnerBindingSource);
        context.Bind(Control.BorderBrushProperty, root, Control.BorderBrushProperty, OwnerBindingSource);
        context.Bind(Control.BorderThicknessProperty, root, Control.BorderThicknessProperty, OwnerBindingSource);
        context.Bind(Control.PaddingProperty, contentHost, UIElement.MarginProperty, OwnerBindingSource);
        return root;
    }
}

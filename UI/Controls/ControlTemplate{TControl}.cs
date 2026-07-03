using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed class ControlTemplate<TControl> : ControlTemplate
    where TControl : Control
{
    private readonly Func<TemplateContext<TControl>, UIElement?> factory;

    public ControlTemplate(Func<TemplateContext<TControl>, UIElement?> factory)
        : base(typeof(TControl))
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    private protected override TemplateInstance CreateInstanceCore(Control owner)
    {
        TemplateContext<TControl> context = new((TControl)owner);
        UIElement? root = factory(context);
        return new TemplateInstance(root, context.Bindings);
    }
}

using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class ControlTemplateAdapter : ComponentTemplate
{
    private readonly ControlTemplate template;

    public ControlTemplateAdapter(ControlTemplate template)
        : base(template?.OwnerType ?? throw new ArgumentNullException(nameof(template)), $"legacy.{template.OwnerType.Name}")
    {
        this.template = template;
    }

    private protected override ComponentTemplateInstance CreateInstanceCore(Control owner, ComponentTemplateContext context)
    {
        TemplateInstance legacy = template.CreateInstance(owner);
        return new ComponentTemplateInstance(legacy.Root, legacy.Bindings, [], new TemplateSlotMap(), new TemplatePartMap());
    }
}

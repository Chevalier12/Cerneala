using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class ComponentAspectBuilder
{
    private readonly List<AspectRuleSet> rules;
    private readonly List<ComponentTemplateDefinition> templates;

    internal ComponentAspectBuilder(List<AspectRuleSet> rules, List<ComponentTemplateDefinition> templates)
    {
        this.rules = rules;
        this.templates = templates;
    }

    public ComponentAspectBuilder AddRule(AspectRuleSet rule)
    {
        rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
        return this;
    }

    public ComponentAspectBuilder AddTemplate(ComponentTemplateDefinition template)
    {
        templates.Add(template ?? throw new ArgumentNullException(nameof(template)));
        return this;
    }
}

using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class ComponentAspectBuilder
{
    private readonly List<AspectRuleSet> rules;
    private readonly List<AspectBehavior> behaviors;
    private readonly List<ComponentTemplateDefinition> templates;

    internal ComponentAspectBuilder(
        List<AspectRuleSet> rules,
        List<AspectBehavior> behaviors,
        List<ComponentTemplateDefinition> templates)
    {
        this.rules = rules;
        this.behaviors = behaviors;
        this.templates = templates;
    }

    public ComponentAspectBuilder AddBehavior(AspectBehavior behavior)
    {
        behaviors.Add(behavior ?? throw new ArgumentNullException(nameof(behavior)));
        return this;
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

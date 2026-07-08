using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectPackageBuilder
{
    private readonly List<AspectTokenDefinition> tokens = [];
    private readonly List<AspectRuleSet> rules = [];
    private readonly List<ComponentTemplateDefinition> componentTemplates = [];
    private readonly List<ContentTemplateDefinition> contentTemplates = [];

    internal AspectPackageBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect package name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    public AspectPackageBuilder Tokens(Action<AspectTokenBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        AspectTokenBuilder builder = new(tokens);
        build(builder);
        return this;
    }

    public AspectPackageBuilder Components(Action<ComponentAspectBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        ComponentAspectBuilder builder = new(rules, componentTemplates);
        build(builder);
        return this;
    }

    public AspectPackageBuilder Content(Action<ContentTemplateBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        ContentTemplateBuilder builder = new(contentTemplates);
        build(builder);
        return this;
    }

    public AspectPackage Build()
    {
        return new AspectPackage(Name, tokens.ToArray(), rules.ToArray(), componentTemplates.ToArray(), contentTemplates.ToArray());
    }

    public static implicit operator AspectPackage(AspectPackageBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Build();
    }
}

public sealed class AspectTokenBuilder
{
    private readonly List<AspectTokenDefinition> tokens;

    internal AspectTokenBuilder(List<AspectTokenDefinition> tokens)
    {
        this.tokens = tokens;
    }

    public AspectTokenBuilder Set<T>(AspectToken<T> token, T value)
    {
        tokens.Add(new AspectTokenDefinition(token, AspectValue<T>.Literal(value)));
        return this;
    }
}

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

public sealed class ContentTemplateBuilder
{
    private readonly List<ContentTemplateDefinition> templates;

    internal ContentTemplateBuilder(List<ContentTemplateDefinition> templates)
    {
        this.templates = templates;
    }

    public ContentTemplateBuilder Add(ContentTemplateDefinition template)
    {
        templates.Add(template ?? throw new ArgumentNullException(nameof(template)));
        return this;
    }
}

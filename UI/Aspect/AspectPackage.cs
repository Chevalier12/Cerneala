using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectPackage
{
    internal AspectPackage(
        string name,
        AspectOrigin origin,
        IReadOnlyList<AspectTokenDefinition> tokens,
        IReadOnlyList<AspectRuleSet> rules,
        IReadOnlyList<AspectBehavior> behaviors,
        IReadOnlyList<ComponentTemplateDefinition> componentTemplates,
        IReadOnlyList<ContentTemplateDefinition> contentTemplates)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect package name cannot be empty.", nameof(name));
        }

        Name = name;
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Tokens = Snapshot(tokens, nameof(tokens));
        Rules = Snapshot(rules, nameof(rules));
        Behaviors = Snapshot(behaviors, nameof(behaviors));
        ComponentTemplates = Snapshot(componentTemplates, nameof(componentTemplates));
        ContentTemplates = Snapshot(contentTemplates, nameof(contentTemplates));
    }

    public string Name { get; }

    public AspectOrigin Origin { get; }

    public IReadOnlyList<AspectTokenDefinition> Tokens { get; }

    public IReadOnlyList<AspectRuleSet> Rules { get; }

    public IReadOnlyList<AspectBehavior> Behaviors { get; }

    public IReadOnlyList<ComponentTemplateDefinition> ComponentTemplates { get; }

    public IReadOnlyList<ContentTemplateDefinition> ContentTemplates { get; }

    public static AspectPackageBuilder Create(string name)
    {
        return new AspectPackageBuilder(name);
    }

    private static IReadOnlyList<T> Snapshot<T>(IReadOnlyList<T> values, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        return Array.AsReadOnly(values.Select(
            value => value ?? throw new ArgumentException("Aspect package collections cannot contain null.", parameterName)).ToArray());
    }
}

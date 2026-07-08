using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectPackage
{
    internal AspectPackage(
        string name,
        IReadOnlyList<AspectTokenDefinition> tokens,
        IReadOnlyList<AspectRuleSet> rules,
        IReadOnlyList<ComponentTemplateDefinition> componentTemplates,
        IReadOnlyList<ContentTemplateDefinition> contentTemplates)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect package name cannot be empty.", nameof(name));
        }

        Name = name;
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        ComponentTemplates = componentTemplates ?? throw new ArgumentNullException(nameof(componentTemplates));
        ContentTemplates = contentTemplates ?? throw new ArgumentNullException(nameof(contentTemplates));
    }

    public string Name { get; }

    public IReadOnlyList<AspectTokenDefinition> Tokens { get; }

    public IReadOnlyList<AspectRuleSet> Rules { get; }

    public IReadOnlyList<ComponentTemplateDefinition> ComponentTemplates { get; }

    public IReadOnlyList<ContentTemplateDefinition> ContentTemplates { get; }

    public static AspectPackageBuilder Create(string name)
    {
        return new AspectPackageBuilder(name);
    }
}

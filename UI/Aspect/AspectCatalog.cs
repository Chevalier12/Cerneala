using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectCatalog
{
    private readonly Dictionary<AspectToken, AspectValue> tokenDefaults;

    private AspectCatalog(
        int version,
        IReadOnlyList<AspectPackageDiagnostic> packageDiagnostics,
        Dictionary<AspectToken, AspectValue> tokenDefaults,
        IReadOnlyList<AspectRuleSet> rules,
        IReadOnlyList<ComponentTemplateDefinition> componentTemplates,
        IReadOnlyList<ContentTemplateDefinition> contentTemplates)
    {
        Version = version;
        PackageDiagnostics = packageDiagnostics;
        this.tokenDefaults = tokenDefaults;
        Rules = rules;
        ComponentTemplates = componentTemplates;
        ContentTemplates = contentTemplates;
    }

    public int Version { get; }

    public IReadOnlyList<AspectPackageDiagnostic> PackageDiagnostics { get; }

    public IReadOnlyList<AspectRuleSet> Rules { get; }

    public IReadOnlyList<ComponentTemplateDefinition> ComponentTemplates { get; }

    public IReadOnlyList<ContentTemplateDefinition> ContentTemplates { get; }

    public IReadOnlyDictionary<AspectToken, AspectValue> TokenDefaults => tokenDefaults;

    public bool TryGetTokenDefault(AspectToken token, out AspectValue value)
    {
        ArgumentNullException.ThrowIfNull(token);
        return tokenDefaults.TryGetValue(token, out value!);
    }

    internal static AspectCatalog FromPackages(IReadOnlyList<AspectPackage> packages, int version)
    {
        Dictionary<AspectToken, AspectValue> tokens = [];
        Dictionary<string, AspectToken> tokensByName = new(StringComparer.Ordinal);
        List<AspectRuleSet> rules = [];
        List<ComponentTemplateDefinition> componentTemplates = [];
        List<ContentTemplateDefinition> contentTemplates = [];
        List<AspectPackageDiagnostic> diagnostics = [];

        foreach (AspectPackage package in packages)
        {
            diagnostics.Add(new AspectPackageDiagnostic(package.Name));
            foreach (AspectTokenDefinition token in package.Tokens)
            {
                if (tokensByName.TryGetValue(token.Token.Name, out AspectToken? existing) &&
                    existing.ValueType != token.Token.ValueType)
                {
                    throw new InvalidOperationException(
                        $"Aspect token '{token.Token.Name}' is registered with both '{existing.ValueType.FullName}' and '{token.Token.ValueType.FullName}'.");
                }

                tokensByName[token.Token.Name] = token.Token;
                tokens[token.Token] = token.DefaultValue;
            }

            rules.AddRange(package.Rules);
            foreach (AspectRuleSet rule in package.Rules)
            {
                rule.PackageName = package.Name;
            }
            componentTemplates.AddRange(package.ComponentTemplates);
            contentTemplates.AddRange(package.ContentTemplates);
        }

        return new AspectCatalog(version, diagnostics, tokens, rules, componentTemplates, contentTemplates);
    }
}

public sealed record AspectPackageDiagnostic(string Name);

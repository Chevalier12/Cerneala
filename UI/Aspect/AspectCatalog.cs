using System.Collections.ObjectModel;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectCatalog
{
    private readonly IReadOnlyDictionary<AspectToken, AspectValue> tokenDefaults;

    private AspectCatalog(
        int version,
        IReadOnlyList<AspectPackageDiagnostic> packageDiagnostics,
        Dictionary<AspectToken, AspectValue> tokenDefaults,
        IReadOnlyList<AspectRuleSet> rules,
        IReadOnlyList<AspectBehavior> behaviors,
        IReadOnlyList<ComponentTemplateDefinition> componentTemplates,
        IReadOnlyList<ContentTemplateDefinition> contentTemplates)
    {
        Version = version;
        PackageDiagnostics = Array.AsReadOnly(packageDiagnostics.ToArray());
        this.tokenDefaults = new ReadOnlyDictionary<AspectToken, AspectValue>(new Dictionary<AspectToken, AspectValue>(tokenDefaults));
        Rules = Array.AsReadOnly(rules.ToArray());
        Behaviors = Array.AsReadOnly(behaviors.ToArray());
        ComponentTemplates = Array.AsReadOnly(componentTemplates.ToArray());
        ContentTemplates = Array.AsReadOnly(contentTemplates.ToArray());
    }

    public int Version { get; }

    public IReadOnlyList<AspectPackageDiagnostic> PackageDiagnostics { get; }

    public IReadOnlyList<AspectRuleSet> Rules { get; }

    public IReadOnlyList<AspectBehavior> Behaviors { get; }

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
        List<AspectBehavior> behaviors = [];
        List<ComponentTemplateDefinition> componentTemplates = [];
        List<ContentTemplateDefinition> contentTemplates = [];
        List<AspectPackageDiagnostic> diagnostics = [];

        foreach (AspectPackage package in packages)
        {
            AppendPackage(
                new AspectPackageSource(package, SourceOrder: 0, Scope: "root"),
                diagnostics,
                tokens,
                tokensByName,
                rules,
                behaviors,
                componentTemplates,
                contentTemplates);
        }

        return new AspectCatalog(version, diagnostics, tokens, rules, behaviors, componentTemplates, contentTemplates);
    }

    internal static AspectCatalog Compose(
        AspectCatalog rootCatalog,
        IReadOnlyList<AspectPackageSource> sources,
        int version)
    {
        ArgumentNullException.ThrowIfNull(rootCatalog);
        ArgumentNullException.ThrowIfNull(sources);
        Dictionary<AspectToken, AspectValue> tokens = rootCatalog.TokenDefaults
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        Dictionary<string, AspectToken> tokensByName = rootCatalog.TokenDefaults.Keys
            .ToDictionary(token => token.Name, StringComparer.Ordinal);
        List<AspectRuleSet> rules = [.. rootCatalog.Rules];
        List<AspectBehavior> behaviors = [.. rootCatalog.Behaviors];
        List<ComponentTemplateDefinition> componentTemplates = [.. rootCatalog.ComponentTemplates];
        List<ContentTemplateDefinition> contentTemplates = [.. rootCatalog.ContentTemplates];
        List<AspectPackageDiagnostic> diagnostics = [.. rootCatalog.PackageDiagnostics];

        foreach (AspectPackageSource source in sources)
        {
            AppendPackage(
                source,
                diagnostics,
                tokens,
                tokensByName,
                rules,
                behaviors,
                componentTemplates,
                contentTemplates);
        }

        return new AspectCatalog(version, diagnostics, tokens, rules, behaviors, componentTemplates, contentTemplates);
    }

    private static void AppendPackage(
        AspectPackageSource source,
        List<AspectPackageDiagnostic> diagnostics,
        Dictionary<AspectToken, AspectValue> tokens,
        Dictionary<string, AspectToken> tokensByName,
        List<AspectRuleSet> rules,
        List<AspectBehavior> behaviors,
        List<ComponentTemplateDefinition> componentTemplates,
        List<ContentTemplateDefinition> contentTemplates)
    {
        AspectPackage package = source.Package;
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

        foreach (AspectRuleSet rule in package.Rules)
        {
            rules.Add(rule.WithOrigin(package.Name, source.SourceOrder, package.Origin, source.Scope));
        }

        behaviors.AddRange(package.Behaviors);
        componentTemplates.AddRange(package.ComponentTemplates);
        contentTemplates.AddRange(package.ContentTemplates);
    }
}

internal readonly record struct AspectPackageSource(
    AspectPackage Package,
    int SourceOrder,
    string Scope);

public sealed record AspectPackageDiagnostic(string Name);

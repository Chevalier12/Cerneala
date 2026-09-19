using System.Collections.ObjectModel;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class AspectCatalog
{
    private readonly IReadOnlyDictionary<AspectToken, AspectValue> tokenDefaults;

    private AspectCatalog(
        int version,
        List<AspectPackageDiagnostic> packageDiagnostics,
        Dictionary<AspectToken, AspectValue> tokenDefaults,
        List<AspectRuleSet> rules,
        List<AspectBehavior> behaviors,
        List<ComponentTemplateDefinition> componentTemplates,
        List<ContentTemplateDefinition> contentTemplates)
    {
        Version = version;
        PackageDiagnostics = packageDiagnostics.AsReadOnly();
        this.tokenDefaults = new ReadOnlyDictionary<AspectToken, AspectValue>(tokenDefaults);
        Rules = rules.AsReadOnly();
        Behaviors = behaviors.AsReadOnly();
        ComponentTemplates = componentTemplates.AsReadOnly();
        ContentTemplates = contentTemplates.AsReadOnly();
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
        CatalogAccumulator accumulator = new();
        foreach (AspectPackage package in packages)
        {
            accumulator.Append(new AspectPackageSource(package, SourceOrder: 0, Scope: "root"));
        }

        return accumulator.Build(version);
    }

    internal static AspectCatalog Compose(
        AspectCatalog rootCatalog,
        IReadOnlyList<AspectPackageSource> sources,
        int version)
    {
        ArgumentNullException.ThrowIfNull(rootCatalog);
        ArgumentNullException.ThrowIfNull(sources);
        CatalogAccumulator accumulator = new(rootCatalog);
        foreach (AspectPackageSource source in sources)
        {
            accumulator.Append(source);
        }

        return accumulator.Build(version);
    }

    private sealed class CatalogAccumulator
    {
        private readonly Dictionary<AspectToken, AspectValue> tokenDefaults = [];
        private readonly Dictionary<string, AspectToken> tokensByName = new(StringComparer.Ordinal);
        private readonly List<AspectRuleSet> rules = [];
        private readonly List<AspectBehavior> behaviors = [];
        private readonly List<ComponentTemplateDefinition> componentTemplates = [];
        private readonly List<ContentTemplateDefinition> contentTemplates = [];
        private readonly List<AspectPackageDiagnostic> diagnostics = [];

        public CatalogAccumulator()
        {
        }

        public CatalogAccumulator(AspectCatalog catalog)
        {
            foreach ((AspectToken token, AspectValue defaultValue) in catalog.TokenDefaults)
            {
                tokenDefaults.Add(token, defaultValue);
                tokensByName.Add(token.Name, token);
            }

            rules.AddRange(catalog.Rules);
            behaviors.AddRange(catalog.Behaviors);
            componentTemplates.AddRange(catalog.ComponentTemplates);
            contentTemplates.AddRange(catalog.ContentTemplates);
            diagnostics.AddRange(catalog.PackageDiagnostics);
        }

        public void Append(AspectPackageSource source)
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
                tokenDefaults[token.Token] = token.DefaultValue;
            }

            foreach (AspectRuleSet rule in package.Rules)
            {
                rules.Add(rule.WithOrigin(package.Name, source.SourceOrder, package.Origin, source.Scope));
            }

            behaviors.AddRange(package.Behaviors);
            componentTemplates.AddRange(package.ComponentTemplates);
            contentTemplates.AddRange(package.ContentTemplates);
        }

        public AspectCatalog Build(int version)
        {
            return new AspectCatalog(
                version,
                diagnostics,
                tokenDefaults,
                rules,
                behaviors,
                componentTemplates,
                contentTemplates);
        }
    }
}

internal readonly record struct AspectPackageSource(
    AspectPackage Package,
    int SourceOrder,
    string Scope);

public sealed record AspectPackageDiagnostic(string Name);

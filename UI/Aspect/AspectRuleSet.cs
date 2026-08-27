using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class AspectRuleSet
{
    public AspectRuleSet(
        string name,
        AspectLayer layer,
        AspectTarget target,
        IReadOnlyList<AspectDeclaration> declarations,
        int declarationOrder)
        : this(
            name,
            layer,
            target,
            declarations,
            declarationOrder,
            packageName: null,
            sourceOrder: 0,
            AspectOrigin.Code(),
            scope: string.Empty)
    {
    }

    private AspectRuleSet(
        string name,
        AspectLayer layer,
        AspectTarget target,
        IReadOnlyList<AspectDeclaration> declarations,
        int declarationOrder,
        string? packageName,
        int sourceOrder,
        AspectOrigin origin,
        string scope)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect rule set name cannot be empty.", nameof(name));
        }

        Name = name;
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ArgumentNullException.ThrowIfNull(declarations);
        Declarations = Array.AsReadOnly(declarations.Select(
            declaration => declaration ?? throw new ArgumentException("Aspect declarations cannot contain null.", nameof(declarations))).ToArray());
        DeclarationOrder = declarationOrder;
        PackageName = packageName;
        SourceOrder = sourceOrder;
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Scope = scope ?? string.Empty;
    }

    public string Name { get; }

    public AspectLayer Layer { get; }

    public AspectTarget Target { get; }

    public IReadOnlyList<AspectDeclaration> Declarations { get; }

    public int DeclarationOrder { get; }

    public string? PackageName { get; }

    public int SourceOrder { get; }

    public AspectOrigin Origin { get; }

    public string Scope { get; }

    internal AspectRuleSet WithOrigin(
        string packageName,
        int sourceOrder,
        AspectOrigin origin,
        string scope)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Aspect package name cannot be empty.", nameof(packageName));
        }

        return new AspectRuleSet(
            Name,
            Layer,
            Target,
            Declarations,
            DeclarationOrder,
            packageName,
            sourceOrder,
            origin,
            scope);
    }

    public bool Matches(AspectMatchContext context)
    {
        return Target.Matches(context);
    }

    public static IReadOnlyDictionary<UiProperty, AspectDeclaration> ResolveDeclarations(
        IEnumerable<AspectRuleSet> rules,
        AspectMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(context);

        Dictionary<UiProperty, (AspectCascadeKey Key, AspectDeclaration Declaration)> winners = new(ReferenceEqualityComparer.Instance);
        foreach (AspectRuleSet rule in rules)
        {
            if (!rule.Matches(context))
            {
                continue;
            }

            AspectCascadeKey key = new(rule.Layer.Order, rule.SourceOrder, rule.Target.Specificity, rule.DeclarationOrder);
            foreach (AspectDeclaration declaration in rule.Declarations)
            {
                if (!winners.TryGetValue(declaration.Property, out (AspectCascadeKey Key, AspectDeclaration Declaration) current) ||
                    key.CompareTo(current.Key) > 0)
                {
                    winners[declaration.Property] = (key, declaration);
                }
            }
        }

        Dictionary<UiProperty, AspectDeclaration> resolved = new(ReferenceEqualityComparer.Instance);
        foreach ((UiProperty property, (AspectCascadeKey Key, AspectDeclaration Declaration) winner) in winners)
        {
            resolved[property] = winner.Declaration;
        }

        return resolved;
    }
}

internal readonly record struct AspectCascadeKey(
    int LayerOrder,
    int SourceOrder,
    AspectSpecificity Specificity,
    int DeclarationOrder) : IComparable<AspectCascadeKey>
{
    public int CompareTo(AspectCascadeKey other)
    {
        int result = LayerOrder.CompareTo(other.LayerOrder);
        if (result != 0)
        {
            return result;
        }

        result = SourceOrder.CompareTo(other.SourceOrder);
        if (result != 0)
        {
            return result;
        }

        result = Specificity.CompareTo(other.Specificity);
        if (result != 0)
        {
            return result;
        }

        return DeclarationOrder.CompareTo(other.DeclarationOrder);
    }
}

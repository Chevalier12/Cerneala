using Cerneala.Language.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cerneala.Language.Semantics.Symbols;

internal sealed class RoslynCompilationSymbols : ILanguageCompilationSymbols
{
    private const int TypeNameCacheCapacity = 256;
    private readonly Compilation compilation;
    private readonly Lazy<IReadOnlyList<ILanguageTypeSymbol>> allTypes;
    private readonly ConditionalWeakTable<ITypeSymbol, RoslynTypeSymbol> typeSymbols = new();
    private readonly ConditionalWeakTable<ITypeSymbol, RoslynTypeSymbol>.CreateValueCallback createTypeSymbol;
    private readonly Dictionary<string, RoslynTypeSymbol?> typeNames = new(StringComparer.Ordinal);
    private readonly Queue<string> typeNameOrder = new();

    public RoslynCompilationSymbols(Compilation compilation, long version = 0)
    {
        this.compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        Version = version;
        createTypeSymbol = symbol => new RoslynTypeSymbol(this, symbol);
        allTypes = new Lazy<IReadOnlyList<ILanguageTypeSymbol>>(CreateAllTypes, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public long Version { get; }

    public ILanguageTypeSymbol? FindType(string metadataName)
    {
        lock (typeNames)
        {
            if (typeNames.TryGetValue(metadataName, out RoslynTypeSymbol? cached))
            {
                return cached;
            }

            INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(metadataName);
            RoslynTypeSymbol? resolved = symbol is null ? null : WrapType(symbol);
            // Missing names are common while typing and during namespace probing.
            // Bound both positive and negative entries; eviction changes cost only.
            if (typeNames.Count == TypeNameCacheCapacity)
            {
                typeNames.Remove(typeNameOrder.Dequeue());
            }

            typeNames.Add(metadataName, resolved);
            typeNameOrder.Enqueue(metadataName);
            return resolved;
        }
    }

    public IReadOnlyList<ILanguageTypeSymbol> FindTypes(string simpleName)
    {
        return compilation.GetSymbolsWithName(
                candidate => string.Equals(candidate, simpleName, StringComparison.Ordinal),
                SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .OrderBy(symbol => symbol.ToDisplayString(), StringComparer.Ordinal)
            .Select(symbol => (ILanguageTypeSymbol)WrapType(symbol))
            .ToArray();
    }

    public IReadOnlyList<ILanguageTypeSymbol> GetTypes() => allTypes.Value;

    public ILanguageTypeSymbol? FindDeclaredTypeForFile(string path, string expectedName)
    {
        string normalized = NormalizePath(path);
        INamedTypeSymbol[] candidates = compilation.SyntaxTrees
            .Where(tree => string.Equals(NormalizePath(tree.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(declaration => string.Equals(declaration.Identifier.ValueText, expectedName, StringComparison.Ordinal))
                .Select(declaration => compilation.GetSemanticModel(tree).GetDeclaredSymbol(declaration)))
            .OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToArray();
        return candidates.Length == 1 ? WrapType(candidates[0]) : null;
    }

    public IReadOnlyList<LanguageReferenceLocation> FindReferences(
        string declaringTypeMetadataName,
        string? memberName,
        LanguageMemberKind? memberKind,
        CancellationToken cancellationToken)
    {
        INamedTypeSymbol? declaringType = compilation.GetTypeByMetadataName(declaringTypeMetadataName);
        ISymbol? target = memberName is null
            ? declaringType
            : declaringType?.GetMembers(memberName).FirstOrDefault(candidate =>
                memberKind is null || ConvertMemberKind(candidate) == memberKind);
        if (target is null)
        {
            return Array.Empty<LanguageReferenceLocation>();
        }

        List<LanguageReferenceLocation> result = ConvertLocations(target.Locations)
            .Select(location => new LanguageReferenceLocation(location.Path, location.Span, isDefinition: true))
            .ToList();
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            foreach (SimpleNameSyntax name in tree.GetRoot(cancellationToken).DescendantNodes().OfType<SimpleNameSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                SymbolInfo info = semanticModel.GetSymbolInfo(name, cancellationToken);
                ISymbol? candidate = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
                if (!IsSameSymbol(candidate, target))
                {
                    continue;
                }

                LanguageReferenceLocation reference = new(
                    tree.FilePath ?? string.Empty,
                    new TextSpan(name.SpanStart, name.Span.Length),
                    isDefinition: false);
                if (!result.Any(existing =>
                    string.Equals(existing.Path, reference.Path, StringComparison.OrdinalIgnoreCase) &&
                    existing.Span.Equals(reference.Span)))
                {
                    result.Add(reference);
                }
            }
        }

        return result;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    // Roslyn type symbols are immutable. Reference identity keeps tuple names and
    // constructed/annotated types distinct; weak keys do not retain transient types.
    private RoslynTypeSymbol WrapType(ITypeSymbol symbol) => typeSymbols.GetValue(symbol, createTypeSymbol);

    private IReadOnlyList<ILanguageTypeSymbol> CreateAllTypes()
    {
        List<INamedTypeSymbol> result = new();
        CollectTypes(compilation.GlobalNamespace, result);
        return result
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .Select(type => (ILanguageTypeSymbol)WrapType(type))
            .OrderBy(type => type.MetadataName, StringComparer.Ordinal)
            .ToArray();
    }

    private static void CollectTypes(INamespaceSymbol @namespace, ICollection<INamedTypeSymbol> destination)
    {
        foreach (INamedTypeSymbol type in @namespace.GetTypeMembers())
        {
            CollectType(type, destination);
        }

        foreach (INamespaceSymbol child in @namespace.GetNamespaceMembers())
        {
            CollectTypes(child, destination);
        }
    }

    private static void CollectType(INamedTypeSymbol type, ICollection<INamedTypeSymbol> destination)
    {
        destination.Add(type);
        foreach (INamedTypeSymbol nested in type.GetTypeMembers())
        {
            CollectType(nested, destination);
        }
    }

    private sealed class RoslynTypeSymbol : ILanguageTypeSymbol
    {
        private readonly RoslynCompilationSymbols owner;
        private readonly ITypeSymbol symbol;
        private string? metadataName;
        private int hasAccessibleParameterlessConstructor = -1;

        public RoslynTypeSymbol(RoslynCompilationSymbols owner, ITypeSymbol symbol)
        {
            this.owner = owner;
            this.symbol = symbol;
        }

        public string Name => symbol.Name;

        public string MetadataName => Volatile.Read(ref metadataName) ??
            LazyInitializer.EnsureInitialized(ref metadataName,
                () => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))!;

        public string AssemblyName => symbol.ContainingAssembly?.Name ?? string.Empty;

        public string Namespace => symbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : string.Empty;

        public LanguageAccessibility Accessibility => ConvertAccessibility(symbol.DeclaredAccessibility);

        public bool IsClass => symbol.TypeKind == TypeKind.Class;

        public bool IsAbstract => symbol.IsAbstract;

        public bool IsEnum => symbol.TypeKind == TypeKind.Enum;

        public bool HasAccessibleParameterlessConstructor
        {
            get
            {
                int cached = Volatile.Read(ref hasAccessibleParameterlessConstructor);
                if (cached >= 0)
                {
                    return cached != 0;
                }

                bool value = symbol is INamedTypeSymbol named && named.InstanceConstructors.Any(constructor =>
                    constructor.Parameters.Length == 0 && IsAccessible(constructor.DeclaredAccessibility));
                Volatile.Write(ref hasAccessibleParameterlessConstructor, value ? 1 : 0);
                return value;
            }
        }

        public string? DocumentationXml => EmptyToNull(symbol.GetDocumentationCommentXml());

        public string? ContentPropertyName
        {
            get
            {
                for (ITypeSymbol? current = symbol; current is not null; current = current.BaseType)
                {
                    AttributeData? attribute = current.GetAttributes().FirstOrDefault(candidate =>
                        candidate.AttributeClass?.ToDisplayString() == "Cerneala.UI.Markup.ContentPropertyAttribute");
                    if (attribute?.ConstructorArguments.Length == 1)
                    {
                        return attribute.ConstructorArguments[0].Value as string;
                    }
                }

                return null;
            }
        }

        public ILanguageTypeSymbol? BaseType =>
            symbol.BaseType is null ? null : owner.WrapType(symbol.BaseType);

        public IReadOnlyList<ILanguageTypeSymbol> TypeArguments => (symbol is INamedTypeSymbol named
            ? named.TypeArguments.AsEnumerable() : Enumerable.Empty<ITypeSymbol>())
            .Select(type => (ILanguageTypeSymbol)owner.WrapType(type))
            .ToArray();

        public ILanguageTypeSymbol? CollectionElementType
        {
            get
            {
                if (symbol is IArrayTypeSymbol array)
                    return owner.WrapType(array.ElementType);
                INamedTypeSymbol? enumerable = symbol.AllInterfaces
                    .Concat(symbol is INamedTypeSymbol named ? new[] { named } : Array.Empty<INamedTypeSymbol>())
                    .FirstOrDefault(candidate =>
                        owner.WrapType(candidate.OriginalDefinition).MetadataName ==
                        "System.Collections.Generic.IEnumerable<T>");
                return enumerable?.TypeArguments.FirstOrDefault() is ITypeSymbol itemType
                    ? owner.WrapType(itemType)
                    : null;
            }
        }

        public IReadOnlyList<ILanguageMemberSymbol> GetMembers(string name)
        {
            List<ILanguageMemberSymbol> members = new();
            for (ITypeSymbol? current = symbol; current is not null; current = current.BaseType)
            {
                members.AddRange(current.GetMembers(name)
                    .Where(member => IsAccessible(member.DeclaredAccessibility))
                    .Select(member => (ILanguageMemberSymbol)new RoslynMemberSymbol(owner, member)));
            }

            return members;
        }

        public IReadOnlyList<ILanguageMemberSymbol> GetMembers()
        {
            List<ILanguageMemberSymbol> members = new();
            for (ITypeSymbol? current = symbol; current is not null; current = current.BaseType)
            {
                members.AddRange(current.GetMembers()
                    .Where(member => IsAccessible(member.DeclaredAccessibility))
                    .Where(member => !member.IsImplicitlyDeclared)
                    .Select(member => (ILanguageMemberSymbol)new RoslynMemberSymbol(owner, member)));
            }

            return members;
        }

        public IReadOnlyList<LanguageSourceLocation> Locations => ConvertLocations(symbol.Locations);

        public bool IsOrDerivesFrom(string metadataName)
        {
            for (ITypeSymbol? current = symbol; current is not null; current = current.BaseType)
            {
                if (string.Equals(
                    owner.WrapType(current).MetadataName,
                    metadataName,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsOrImplements(string metadataName)
        {
            if (IsOrDerivesFrom(metadataName))
            {
                return true;
            }

            return symbol.AllInterfaces.Any(candidate => string.Equals(
                owner.WrapType(candidate).MetadataName,
                metadataName,
                StringComparison.Ordinal));
        }
    }

    private sealed class RoslynMemberSymbol : ILanguageMemberSymbol
    {
        private readonly ISymbol symbol;

        private readonly RoslynCompilationSymbols owner;

        public RoslynMemberSymbol(RoslynCompilationSymbols owner, ISymbol symbol)
        {
            this.owner = owner;
            this.symbol = symbol;
        }

        public string Name => symbol.Name;

        public LanguageMemberKind Kind => symbol switch
        {
            IPropertySymbol => LanguageMemberKind.Property,
            IEventSymbol => LanguageMemberKind.Event,
            IMethodSymbol => LanguageMemberKind.Method,
            _ => LanguageMemberKind.Field
        };

        public LanguageAccessibility Accessibility => ConvertAccessibility(symbol.DeclaredAccessibility);

        public bool IsStatic => symbol.IsStatic;

        public bool CanRead => symbol is not IPropertySymbol property || property.GetMethod is not null;

        public bool CanWrite => symbol is IPropertySymbol property && property.SetMethod is { IsInitOnly: false };

        public string ValueTypeMetadataName => GetValueType(symbol) is ITypeSymbol type
            ? owner.WrapType(type).MetadataName
            : "System.Object";

        public ILanguageTypeSymbol? ValueType => GetValueType(symbol) is ITypeSymbol type
            ? owner.WrapType(type)
            : null;

        public IReadOnlyList<string> EnumValues
        {
            get
            {
                ITypeSymbol? type = GetValueType(symbol);
                return type?.TypeKind == TypeKind.Enum
                    ? type.GetMembers().OfType<IFieldSymbol>().Where(enumField => enumField.HasConstantValue).Select(enumField => enumField.Name).ToArray()
                    : [];
            }
        }

        public string DeclaringTypeMetadataName =>
            symbol.ContainingType is ITypeSymbol type ? owner.WrapType(type).MetadataName : string.Empty;

        public string AssemblyName => symbol.ContainingAssembly?.Name ?? string.Empty;

        public string Signature => symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        public bool IsDeprecated => symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");

        public string? DefaultValue
        {
            get
            {
                AttributeData? attribute = symbol.GetAttributes().FirstOrDefault(candidate =>
                    candidate.AttributeClass?.ToDisplayString() == "System.ComponentModel.DefaultValueAttribute");
                return attribute?.ConstructorArguments.Length == 1
                    ? FormatConstant(attribute.ConstructorArguments[0])
                    : null;
            }
        }

        public IReadOnlyList<LanguageParameterSymbol> Parameters => symbol is IMethodSymbol method
            ? method.Parameters.Select(parameter => new LanguageParameterSymbol(
                parameter.Name,
                owner.WrapType(parameter.Type).MetadataName,
                parameter.IsOptional)).ToArray()
            : Array.Empty<LanguageParameterSymbol>();

        public string? DocumentationXml => EmptyToNull(symbol.GetDocumentationCommentXml());

        public IReadOnlyList<LanguageSourceLocation> Locations => ConvertLocations(symbol.Locations);

        private static ITypeSymbol? GetValueType(ISymbol value) => value switch
        {
            IPropertySymbol property => property.Type,
            IEventSymbol @event => @event.Type,
            IFieldSymbol field => field.Type,
            IMethodSymbol method => method.Parameters.Length > 0
                ? method.Parameters[method.Parameters.Length - 1].Type
                : method.ReturnType,
            _ => null
        };
    }

    private static IReadOnlyList<LanguageSourceLocation> ConvertLocations(IEnumerable<Location> locations)
    {
        return locations.Where(location => location.IsInSource)
            .Select(location => new LanguageSourceLocation(
                location.SourceTree?.FilePath ?? string.Empty,
                new TextSpan(location.SourceSpan.Start, location.SourceSpan.Length)))
            .ToArray();
    }

    private static bool IsAccessible(Accessibility accessibility) => accessibility is
        Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

    private static LanguageMemberKind ConvertMemberKind(ISymbol symbol) => symbol switch
    {
        IPropertySymbol => LanguageMemberKind.Property,
        IEventSymbol => LanguageMemberKind.Event,
        IMethodSymbol => LanguageMemberKind.Method,
        _ => LanguageMemberKind.Field
    };

    private static bool IsSameSymbol(ISymbol? candidate, ISymbol target)
    {
        if (candidate is IAliasSymbol alias)
        {
            candidate = alias.Target;
        }

        if (candidate is IMethodSymbol { ReducedFrom: not null } method)
        {
            candidate = method.ReducedFrom;
        }

        return candidate is not null && SymbolEqualityComparer.Default.Equals(
            candidate.OriginalDefinition,
            target.OriginalDefinition);
    }

    private static string? FormatConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        return constant.Value switch
        {
            bool value => value ? "true" : "false",
            string value => "\"" + value.Replace("\"", "\\\"") + "\"",
            char value => "'" + value.ToString() + "'",
            IFormattable value => value.ToString(null, CultureInfo.InvariantCulture),
            object value => value.ToString(),
            _ => null
        };
    }

    private static LanguageAccessibility ConvertAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => LanguageAccessibility.Public,
        Accessibility.Internal => LanguageAccessibility.Internal,
        Accessibility.Protected => LanguageAccessibility.Protected,
        Accessibility.ProtectedOrInternal => LanguageAccessibility.ProtectedInternal,
        Accessibility.Private => LanguageAccessibility.Private,
        _ => LanguageAccessibility.NotApplicable
    };

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

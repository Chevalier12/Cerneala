using Cerneala.Language.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Cerneala.Language.Semantics.Symbols;

internal sealed class RoslynCompilationSymbols : ILanguageCompilationSymbols
{
    private readonly Compilation compilation;
    private readonly Lazy<IReadOnlyList<ILanguageTypeSymbol>> allTypes;

    public RoslynCompilationSymbols(Compilation compilation, long version = 0)
    {
        this.compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        Version = version;
        allTypes = new Lazy<IReadOnlyList<ILanguageTypeSymbol>>(CreateAllTypes, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public long Version { get; }

    public ILanguageTypeSymbol? FindType(string metadataName)
    {
        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(metadataName);
        return symbol is null ? null : new RoslynTypeSymbol(compilation, symbol);
    }

    public IReadOnlyList<ILanguageTypeSymbol> FindTypes(string simpleName)
    {
        return compilation.GetSymbolsWithName(
                candidate => string.Equals(candidate, simpleName, StringComparison.Ordinal),
                SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .OrderBy(symbol => symbol.ToDisplayString(), StringComparer.Ordinal)
            .Select(symbol => (ILanguageTypeSymbol)new RoslynTypeSymbol(compilation, symbol))
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
        return candidates.Length == 1 ? new RoslynTypeSymbol(compilation, candidates[0]) : null;
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

    private IReadOnlyList<ILanguageTypeSymbol> CreateAllTypes()
    {
        List<INamedTypeSymbol> result = new();
        CollectTypes(compilation.GlobalNamespace, result);
        return result
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .OrderBy(type => type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), StringComparer.Ordinal)
            .Select(type => (ILanguageTypeSymbol)new RoslynTypeSymbol(compilation, type))
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
        private readonly Compilation compilation;
        private readonly ITypeSymbol symbol;

        public RoslynTypeSymbol(Compilation compilation, ITypeSymbol symbol)
        {
            this.compilation = compilation;
            this.symbol = symbol;
        }

        public string Name => symbol.Name;

        public string MetadataName => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        public string AssemblyName => symbol.ContainingAssembly?.Name ?? string.Empty;

        public string Namespace => symbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : string.Empty;

        public LanguageAccessibility Accessibility => ConvertAccessibility(symbol.DeclaredAccessibility);

        public bool IsClass => symbol.TypeKind == TypeKind.Class;

        public bool IsAbstract => symbol.IsAbstract;

        public bool IsEnum => symbol.TypeKind == TypeKind.Enum;

        public bool HasAccessibleParameterlessConstructor =>
            symbol is INamedTypeSymbol named && named.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                IsAccessible(constructor.DeclaredAccessibility));

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
            symbol.BaseType is null ? null : new RoslynTypeSymbol(compilation, symbol.BaseType);

        public IReadOnlyList<ILanguageTypeSymbol> TypeArguments => (symbol is INamedTypeSymbol named
            ? named.TypeArguments.AsEnumerable() : Enumerable.Empty<ITypeSymbol>())
            .Select(type => (ILanguageTypeSymbol)new RoslynTypeSymbol(compilation, type))
            .ToArray();

        public ILanguageTypeSymbol? CollectionElementType
        {
            get
            {
                if (symbol is IArrayTypeSymbol array)
                    return new RoslynTypeSymbol(compilation, array.ElementType);
                INamedTypeSymbol? enumerable = symbol.AllInterfaces
                    .Concat(symbol is INamedTypeSymbol named ? new[] { named } : Array.Empty<INamedTypeSymbol>())
                    .FirstOrDefault(candidate =>
                        candidate.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ==
                        "System.Collections.Generic.IEnumerable<T>");
                return enumerable?.TypeArguments.FirstOrDefault() is ITypeSymbol itemType
                    ? new RoslynTypeSymbol(compilation, itemType)
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
                    .Select(member => (ILanguageMemberSymbol)new RoslynMemberSymbol(compilation, member)));
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
                    .Select(member => (ILanguageMemberSymbol)new RoslynMemberSymbol(compilation, member)));
            }

            return members;
        }

        public IReadOnlyList<LanguageSourceLocation> Locations => ConvertLocations(symbol.Locations);

        public bool IsOrDerivesFrom(string metadataName)
        {
            for (ITypeSymbol? current = symbol; current is not null; current = current.BaseType)
            {
                if (string.Equals(
                    current.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
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
                candidate.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                metadataName,
                StringComparison.Ordinal));
        }
    }

    private sealed class RoslynMemberSymbol : ILanguageMemberSymbol
    {
        private readonly ISymbol symbol;

        private readonly Compilation compilation;

        public RoslynMemberSymbol(Compilation compilation, ISymbol symbol)
        {
            this.compilation = compilation;
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

        public string ValueTypeMetadataName => GetValueType(symbol)?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "System.Object";

        public ILanguageTypeSymbol? ValueType => GetValueType(symbol) is ITypeSymbol type
            ? new RoslynTypeSymbol(compilation, type)
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
            symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty;

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
                parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
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

using Cerneala.Language.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cerneala.Language.Semantics.Symbols;

internal sealed class RoslynCompilationSymbols : ILanguageCompilationSymbols
{
    private readonly Compilation compilation;

    public RoslynCompilationSymbols(Compilation compilation, long version = 0)
    {
        this.compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        Version = version;
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

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private sealed class RoslynTypeSymbol : ILanguageTypeSymbol
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol symbol;

        public RoslynTypeSymbol(Compilation compilation, INamedTypeSymbol symbol)
        {
            this.compilation = compilation;
            this.symbol = symbol;
        }

        public string Name => symbol.Name;

        public string MetadataName => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        public string AssemblyName => symbol.ContainingAssembly?.Name ?? string.Empty;

        public LanguageAccessibility Accessibility => ConvertAccessibility(symbol.DeclaredAccessibility);

        public bool IsClass => symbol.TypeKind == TypeKind.Class;

        public bool IsAbstract => symbol.IsAbstract;

        public bool IsEnum => symbol.TypeKind == TypeKind.Enum;

        public bool HasAccessibleParameterlessConstructor =>
            symbol.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                IsAccessible(constructor.DeclaredAccessibility));

        public string? DocumentationXml => EmptyToNull(symbol.GetDocumentationCommentXml());

        public string? ContentPropertyName
        {
            get
            {
                for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
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

        public IReadOnlyList<ILanguageTypeSymbol> TypeArguments => symbol.TypeArguments
            .OfType<INamedTypeSymbol>()
            .Select(type => (ILanguageTypeSymbol)new RoslynTypeSymbol(compilation, type))
            .ToArray();

        public ILanguageTypeSymbol? CollectionElementType
        {
            get
            {
                INamedTypeSymbol? enumerable = symbol.AllInterfaces
                    .Append(symbol)
                    .FirstOrDefault(candidate =>
                        candidate.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ==
                        "System.Collections.Generic.IEnumerable<T>");
                return enumerable?.TypeArguments.FirstOrDefault() is INamedTypeSymbol itemType
                    ? new RoslynTypeSymbol(compilation, itemType)
                    : null;
            }
        }

        public IReadOnlyList<ILanguageMemberSymbol> GetMembers(string name)
        {
            List<ILanguageMemberSymbol> members = new();
            for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
            {
                members.AddRange(current.GetMembers(name)
                    .Where(member => IsAccessible(member.DeclaredAccessibility))
                    .Select(member => (ILanguageMemberSymbol)new RoslynMemberSymbol(compilation, member)));
            }

            return members;
        }

        public IReadOnlyList<LanguageSourceLocation> Locations => ConvertLocations(symbol.Locations);

        public bool IsOrDerivesFrom(string metadataName)
        {
            for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
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

        public bool CanWrite => symbol is IPropertySymbol property && property.SetMethod is not null;

        public string ValueTypeMetadataName => GetValueType(symbol)?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "System.Object";

        public ILanguageTypeSymbol? ValueType => GetValueType(symbol) is INamedTypeSymbol type
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

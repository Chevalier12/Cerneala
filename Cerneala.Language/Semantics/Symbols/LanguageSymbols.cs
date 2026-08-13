using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics.Symbols;

internal enum LanguageAccessibility
{
    NotApplicable,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    Public
}

internal enum LanguageMemberKind
{
    Property,
    Event,
    Method,
    Field
}

internal readonly struct LanguageSourceLocation
{
    public LanguageSourceLocation(string path, TextSpan span)
    {
        Path = path;
        Span = span;
    }

    public string Path { get; }

    public TextSpan Span { get; }
}

internal interface ILanguageCompilationSymbols
{
    long Version { get; }

    ILanguageTypeSymbol? FindType(string metadataName);

    IReadOnlyList<ILanguageTypeSymbol> FindTypes(string simpleName);

    ILanguageTypeSymbol? FindDeclaredTypeForFile(string path, string expectedName);
}

internal interface ILanguageTypeSymbol
{
    string Name { get; }

    string MetadataName { get; }

    string AssemblyName { get; }

    LanguageAccessibility Accessibility { get; }

    bool IsClass { get; }

    bool IsAbstract { get; }

    bool IsEnum { get; }

    bool HasAccessibleParameterlessConstructor { get; }

    string? DocumentationXml { get; }

    string? ContentPropertyName { get; }

    ILanguageTypeSymbol? BaseType { get; }

    IReadOnlyList<ILanguageTypeSymbol> TypeArguments { get; }

    ILanguageTypeSymbol? CollectionElementType { get; }

    IReadOnlyList<ILanguageMemberSymbol> GetMembers(string name);

    IReadOnlyList<LanguageSourceLocation> Locations { get; }

    bool IsOrDerivesFrom(string metadataName);

    bool IsOrImplements(string metadataName);
}

internal interface ILanguageMemberSymbol
{
    string Name { get; }

    LanguageMemberKind Kind { get; }

    LanguageAccessibility Accessibility { get; }

    bool IsStatic { get; }

    bool CanRead { get; }

    bool CanWrite { get; }

    string ValueTypeMetadataName { get; }

    ILanguageTypeSymbol? ValueType { get; }

    IReadOnlyList<string> EnumValues { get; }

    string? DocumentationXml { get; }

    IReadOnlyList<LanguageSourceLocation> Locations { get; }
}

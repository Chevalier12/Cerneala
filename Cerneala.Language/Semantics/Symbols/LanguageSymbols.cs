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

// Mirrors Cerneala.UI.Markup.MarkupValueConstraint, read from [MarkupValueConstraint] on a property.
internal enum LanguageValueConstraint
{
    None,
    NonNegative,
    Positive
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

internal readonly struct LanguageReferenceLocation
{
    public LanguageReferenceLocation(string path, TextSpan span, bool isDefinition)
    {
        Path = path;
        Span = span;
        IsDefinition = isDefinition;
    }

    public string Path { get; }

    public TextSpan Span { get; }

    public bool IsDefinition { get; }
}

internal readonly record struct LanguageParameterSymbol(
    string Name,
    string TypeMetadataName,
    bool IsOptional);

internal interface ILanguageCompilationSymbols
{
    long Version { get; }

    // True when the code the source generator emits for a markup file can call or read this member. The generator
    // checks a markup file paired with a C# class (pairType) with Roslyn's accessibility check from that class, which
    // honors [InternalsVisibleTo]; any other markup file reaches public members and the non-public ones of this assembly.
    bool IsAccessibleFromGeneratedCode(ILanguageMemberSymbol member, ILanguageTypeSymbol? pairType);

    ILanguageTypeSymbol? FindType(string metadataName);

    IReadOnlyList<ILanguageTypeSymbol> FindTypes(string simpleName);

    IReadOnlyList<ILanguageTypeSymbol> GetTypes();

    ILanguageTypeSymbol? FindDeclaredTypeForFile(string path, string expectedName);

    IReadOnlyList<LanguageReferenceLocation> FindReferences(
        string declaringTypeMetadataName,
        string? memberName,
        LanguageMemberKind? memberKind,
        CancellationToken cancellationToken);
}

internal interface ILanguageTypeSymbol
{
    string Name { get; }

    string MetadataName { get; }

    string AssemblyName { get; }

    string Namespace { get; }

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

    IReadOnlyList<ILanguageMemberSymbol> GetMembers();

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

    // True when the value type is an enum marked [Flags]: a markup value may then list several names separated by commas.
    bool IsFlagsEnum { get; }

    // The [MarkupValueConstraint] of a property, for example NonNegative on UIElement.Width; None for other members.
    LanguageValueConstraint ValueConstraint { get; }

    string DeclaringTypeMetadataName { get; }

    string AssemblyName { get; }

    string Signature { get; }

    bool IsDeprecated { get; }

    string? DefaultValue { get; }

    IReadOnlyList<LanguageParameterSymbol> Parameters { get; }

    string? DocumentationXml { get; }

    IReadOnlyList<LanguageSourceLocation> Locations { get; }
}

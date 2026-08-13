using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal enum CernealaSemanticSymbolKind
{
    RootType,
    Element,
    PropertyElement,
    Property,
    AttachedProperty,
    Event,
    Name,
    Resource,
    ResourceReference,
    TypeReference,
    BindingSource,
    BindingSegment,
    BindingMode,
    ContentOwner,
    ContentTemplate,
    TemplatePart,
    Aspect,
    AspectAssignment,
    AspectCondition,
    AspectApplication,
    MotionDirective,
    MotionTarget,
    MotionEvent,
    MotionProperty,
    MotionSpec,
    MotionComposition,
    MotionLifecycle,
    MotionParameter,
    MotionHandle,
    PrismDirective,
    PrismComposition,
    PrismNode,
    PrismOperation,
    PrismProperty,
    PrismParameter,
    PrismValue
}

internal sealed class CernealaSemanticSymbol
{
    public CernealaSemanticSymbol(
        CernealaSemanticSymbolKind kind,
        string name,
        string valueType,
        TextSpan span,
        ILanguageTypeSymbol? typeSymbol = null,
        ILanguageMemberSymbol? memberSymbol = null,
        object? value = null,
        string? contentPropertyName = null,
        LanguageSourceLocation? definitionLocation = null,
        bool isWritable = false)
    {
        Kind = kind;
        Name = name;
        ValueType = valueType;
        Span = span;
        TypeSymbol = typeSymbol;
        MemberSymbol = memberSymbol;
        Value = value;
        ContentPropertyName = contentPropertyName;
        DefinitionLocation = definitionLocation;
        IsWritable = isWritable;
    }

    public CernealaSemanticSymbolKind Kind { get; }

    public string Name { get; }

    public string ValueType { get; }

    public TextSpan Span { get; }

    public ILanguageTypeSymbol? TypeSymbol { get; }

    public ILanguageMemberSymbol? MemberSymbol { get; }

    public object? Value { get; }

    public string? ContentPropertyName { get; }

    public LanguageSourceLocation? DefinitionLocation { get; }

    public bool IsWritable { get; }
}

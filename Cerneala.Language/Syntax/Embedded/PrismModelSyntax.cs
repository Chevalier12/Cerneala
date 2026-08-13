using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

internal enum PrismContainerModelKind
{
    Composition,
    Layer,
    Group,
    Backdrop
}

internal enum PrismOperationModelKind
{
    Filter,
    Style,
    Mask
}

internal enum PrismValueModelKind
{
    Identifier,
    ResourceReference,
    StringLiteral,
    NumberLiteral,
    BooleanLiteral,
    ColorLiteral,
    TupleLiteral,
    NullLiteral
}

internal abstract class PrismMemberModelSyntax
{
    protected PrismMemberModelSyntax(TextSpan span)
    {
        Span = span;
    }

    public TextSpan Span { get; }
}

internal sealed class PrismCompositionModelSyntax
{
    public PrismCompositionModelSyntax(IReadOnlyList<PrismMemberModelSyntax> members, TextSpan span)
    {
        Members = members;
        Span = span;
    }

    public IReadOnlyList<PrismMemberModelSyntax> Members { get; }

    public TextSpan Span { get; }
}

internal sealed class PrismContainerModelSyntax : PrismMemberModelSyntax
{
    public PrismContainerModelSyntax(
        PrismContainerModelKind kind,
        string? name,
        TextSpan nameSpan,
        IReadOnlyList<PrismMemberModelSyntax> members,
        TextSpan span) : base(span)
    {
        Kind = kind;
        Name = name;
        NameSpan = nameSpan;
        Members = members;
    }

    public PrismContainerModelKind Kind { get; }

    public string? Name { get; }

    public TextSpan NameSpan { get; }

    public IReadOnlyList<PrismMemberModelSyntax> Members { get; }
}

internal sealed class PrismOperationModelSyntax : PrismMemberModelSyntax
{
    public PrismOperationModelSyntax(
        PrismOperationModelKind kind,
        string? typeName,
        TextSpan typeSpan,
        IReadOnlyList<PrismMemberModelSyntax> members,
        TextSpan span) : base(span)
    {
        Kind = kind;
        TypeName = typeName;
        TypeSpan = typeSpan;
        Members = members;
    }

    public PrismOperationModelKind Kind { get; }

    public string? TypeName { get; }

    public TextSpan TypeSpan { get; }

    public IReadOnlyList<PrismMemberModelSyntax> Members { get; }
}

internal sealed class PrismParameterModelSyntax : PrismMemberModelSyntax
{
    public PrismParameterModelSyntax(
        string name,
        TextSpan nameSpan,
        string typeName,
        TextSpan typeSpan,
        PrismValueModelSyntax? defaultValue,
        TextSpan span) : base(span)
    {
        Name = name;
        NameSpan = nameSpan;
        TypeName = typeName;
        TypeSpan = typeSpan;
        DefaultValue = defaultValue;
    }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public string TypeName { get; }

    public TextSpan TypeSpan { get; }

    public PrismValueModelSyntax? DefaultValue { get; }
}

internal sealed class PrismAssignmentModelSyntax : PrismMemberModelSyntax
{
    public PrismAssignmentModelSyntax(string name, TextSpan nameSpan, PrismValueModelSyntax value, TextSpan span)
        : base(span)
    {
        Name = name;
        NameSpan = nameSpan;
        Value = value;
    }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public PrismValueModelSyntax Value { get; }
}

internal sealed class PrismValueModelSyntax
{
    public PrismValueModelSyntax(string text, PrismValueModelKind kind, TextSpan span)
    {
        Text = text;
        Kind = kind;
        Span = span;
    }

    public string Text { get; }

    public PrismValueModelKind Kind { get; }

    public TextSpan Span { get; }
}

internal sealed class PrismApplicationModelSyntax
{
    public PrismApplicationModelSyntax(
        string? resourceName,
        TextSpan resourceSpan,
        IReadOnlyList<PrismAssignmentModelSyntax> arguments,
        PrismCompositionModelSyntax? composition,
        TextSpan span)
    {
        ResourceName = resourceName;
        ResourceSpan = resourceSpan;
        Arguments = arguments;
        Composition = composition;
        Span = span;
    }

    public string? ResourceName { get; }

    public TextSpan ResourceSpan { get; }

    public IReadOnlyList<PrismAssignmentModelSyntax> Arguments { get; }

    public PrismCompositionModelSyntax? Composition { get; }

    public TextSpan Span { get; }
}

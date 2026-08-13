using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

internal enum BindingValueKind
{
    Literal,
    Direct,
    Interpolation,
    Invalid
}

internal enum BindingModeSyntax
{
    OneWay,
    TwoWay
}

internal sealed class BindingValueSyntax
{
    public BindingValueSyntax(
        BindingValueKind kind,
        string text,
        TextSpan span,
        BindingPathSyntax? binding,
        IReadOnlyList<InterpolationFragmentSyntax> fragments)
    {
        Kind = kind;
        Text = text;
        Span = span;
        Binding = binding;
        Fragments = fragments;
    }

    public BindingValueKind Kind { get; }

    public string Text { get; }

    public TextSpan Span { get; }

    public BindingPathSyntax? Binding { get; }

    public IReadOnlyList<InterpolationFragmentSyntax> Fragments { get; }
}

internal sealed class BindingPathSyntax
{
    public BindingPathSyntax(
        string text,
        TextSpan span,
        IReadOnlyList<BindingPathSegmentSyntax> segments,
        BindingModeSyntax mode,
        TextSpan modeSpan)
    {
        Text = text;
        Span = span;
        Segments = segments;
        Mode = mode;
        ModeSpan = modeSpan;
    }

    public string Text { get; }

    public TextSpan Span { get; }

    public IReadOnlyList<BindingPathSegmentSyntax> Segments { get; }

    public BindingModeSyntax Mode { get; }

    public TextSpan ModeSpan { get; }
}

internal sealed class BindingPathSegmentSyntax
{
    public BindingPathSegmentSyntax(string name, TextSpan span)
    {
        Name = name;
        Span = span;
    }

    public string Name { get; }

    public TextSpan Span { get; }
}

internal abstract class InterpolationFragmentSyntax
{
    protected InterpolationFragmentSyntax(TextSpan span)
    {
        Span = span;
    }

    public TextSpan Span { get; }
}

internal sealed class LiteralFragmentSyntax : InterpolationFragmentSyntax
{
    public LiteralFragmentSyntax(string text, TextSpan span) : base(span)
    {
        Text = text;
    }

    public string Text { get; }
}

internal sealed class BindingFragmentSyntax : InterpolationFragmentSyntax
{
    public BindingFragmentSyntax(BindingPathSyntax binding) : base(binding.Span)
    {
        Binding = binding;
    }

    public BindingPathSyntax Binding { get; }
}

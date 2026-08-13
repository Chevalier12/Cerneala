using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

internal static class BindingSyntaxParser
{
    public static EmbeddedParseResult<BindingValueSyntax> Parse(string text, int absoluteOffset = 0)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        List<EmbeddedDiagnostic> diagnostics = new();
        if (text.Length > 0 && text[0] == '$')
        {
            PathParse path = ParsePath(text, 0, absoluteOffset, diagnostics);
            BindingValueKind kind = path.Binding is not null && path.Length == text.Length && diagnostics.Count == 0
                ? BindingValueKind.Direct
                : BindingValueKind.Invalid;
            if (path.Length < text.Length && diagnostics.Count == 0)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    "CERNEALAUI007",
                    "A binding must be one unquoted path token with an optional final :OneWay or :TwoWay suffix.",
                    new TextSpan(absoluteOffset + path.Length, text.Length - path.Length)));
            }

            return new EmbeddedParseResult<BindingValueSyntax>(
                new BindingValueSyntax(
                    kind,
                    text,
                    new TextSpan(absoluteOffset, text.Length),
                    path.Binding,
                    []),
                diagnostics);
        }

        List<InterpolationFragmentSyntax> fragments = new();
        int literalStart = 0;
        int position = 0;
        while (position < text.Length)
        {
            if (text[position] == '\\' && position + 1 < text.Length && text[position + 1] == '$')
            {
                position += 2;
                continue;
            }

            if (text[position] != '$')
            {
                position++;
                continue;
            }

            if (position + 1 >= text.Length || !IsIdentifierStart(text[position + 1]))
            {
                position++;
                continue;
            }

            if (position > literalStart)
            {
                fragments.Add(new LiteralFragmentSyntax(
                    text.Substring(literalStart, position - literalStart),
                    new TextSpan(absoluteOffset + literalStart, position - literalStart)));
            }

            PathParse path = ParsePath(text, position, absoluteOffset, diagnostics);
            if (path.Binding is null || path.Length == 0)
            {
                position++;
                literalStart = position - 1;
                continue;
            }

            if (path.Binding.ModeSpan.Length > 0)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    "CERNEALAUI007",
                    "Binding modes are not allowed inside an interpolated string.",
                    path.Binding.ModeSpan));
            }

            fragments.Add(new BindingFragmentSyntax(path.Binding));
            position += path.Length;
            literalStart = position;
        }

        if (literalStart < text.Length)
        {
            fragments.Add(new LiteralFragmentSyntax(
                text.Substring(literalStart),
                new TextSpan(absoluteOffset + literalStart, text.Length - literalStart)));
        }

        BindingValueKind valueKind = fragments.OfType<BindingFragmentSyntax>().Any()
            ? diagnostics.Count == 0 ? BindingValueKind.Interpolation : BindingValueKind.Invalid
            : BindingValueKind.Literal;
        return new EmbeddedParseResult<BindingValueSyntax>(
            new BindingValueSyntax(valueKind, text, new TextSpan(absoluteOffset, text.Length), null, fragments),
            diagnostics);
    }

    private static PathParse ParsePath(
        string text,
        int start,
        int absoluteOffset,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        int position = start + 1;
        List<BindingPathSegmentSyntax> segments = new();
        while (true)
        {
            int segmentStart = position;
            if (position < text.Length && text[position] == '$')
            {
                position++;
            }

            if (position >= text.Length || !IsIdentifierStart(text[position]))
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    "CERNEALAUI007",
                    "A binding path segment requires an identifier.",
                    new TextSpan(absoluteOffset + Math.Min(position, text.Length), 0),
                    transient: position >= text.Length));
                return new PathParse(null, Math.Max(1, position - start));
            }

            position++;
            while (position < text.Length && IsIdentifierPart(text[position]))
            {
                position++;
            }

            segments.Add(new BindingPathSegmentSyntax(
                text.Substring(segmentStart, position - segmentStart),
                new TextSpan(absoluteOffset + segmentStart, position - segmentStart)));
            if (position >= text.Length || text[position] != '.')
            {
                break;
            }

            position++;
            if (position >= text.Length)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    "CERNEALAUI007",
                    "A binding path token cannot end with '.'.",
                    new TextSpan(absoluteOffset + position, 0),
                    transient: true));
                return new PathParse(null, position - start);
            }
        }

        BindingModeSyntax mode = BindingModeSyntax.OneWay;
        TextSpan modeSpan = new(absoluteOffset + position, 0);
        if (position < text.Length && text[position] == ':')
        {
            int modeStart = position++;
            int wordStart = position;
            while (position < text.Length && char.IsLetter(text[position]))
            {
                position++;
            }

            string modeText = text.Substring(wordStart, position - wordStart);
            modeSpan = new TextSpan(absoluteOffset + modeStart, position - modeStart);
            if (string.Equals(modeText, "TwoWay", StringComparison.Ordinal))
            {
                mode = BindingModeSyntax.TwoWay;
            }
            else if (!string.Equals(modeText, "OneWay", StringComparison.Ordinal))
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    "CERNEALAUI007",
                    "Unknown binding mode '" + modeText + "'. Expected OneWay or TwoWay.",
                    modeSpan,
                    transient: position == text.Length));
            }
        }

        string pathText = text.Substring(start, position - start);
        return new PathParse(
            new BindingPathSyntax(
                pathText,
                new TextSpan(absoluteOffset + start, position - start),
                segments,
                mode,
                modeSpan),
            position - start);
    }

    private static bool IsIdentifierStart(char character) => char.IsLetter(character) || character == '_';

    private static bool IsIdentifierPart(char character) => char.IsLetterOrDigit(character) || character == '_';

    private readonly struct PathParse
    {
        public PathParse(BindingPathSyntax? binding, int length)
        {
            Binding = binding;
            Length = length;
        }

        public BindingPathSyntax? Binding { get; }

        public int Length { get; }
    }
}

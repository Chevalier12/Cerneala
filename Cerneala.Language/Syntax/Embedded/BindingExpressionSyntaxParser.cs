using System.Globalization;
using System.Net;
using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

// What a property value means for the markup generator's property-expression rule
// (UiMarkupGenerator.ParseMarkupExpression), for example IsEnabled="$DataContext.A and $DataContext.B".
internal enum BindingExpressionShape
{
    // The value keeps its existing meaning: a literal, a direct binding, a reference or an interpolation.
    None,

    // A bool target with an expression that parses with the @if/@when grammar and has no mode suffix.
    Expression,

    // A bool target with an expression the generator rejects with its own message
    // (a syntax error, or a binding mode inside or after the expression).
    Rejected,

    // An expression with 'and', 'or' or a comparison on a target that is not bool.
    NonBooleanTarget
}

internal sealed class BindingExpressionSyntax
{
    public BindingExpressionSyntax(
        string text,
        TextSpan span,
        bool isWholeBindingToken,
        bool parsed,
        bool isSingleSource,
        bool hasOperator,
        bool hasModeSeparator,
        IReadOnlyList<BindingPathSyntax> sources)
    {
        Text = text;
        Span = span;
        IsWholeBindingToken = isWholeBindingToken;
        Parsed = parsed;
        IsSingleSource = isSingleSource;
        HasOperator = hasOperator;
        HasModeSeparator = hasModeSeparator;
        Sources = sources;
    }

    // The entity-decoded, trimmed value: the text the generator parses.
    public string Text { get; }

    // The document span of the trimmed value.
    public TextSpan Span { get; }

    public bool IsWholeBindingToken { get; }

    public bool Parsed { get; }

    public bool IsSingleSource { get; }

    public bool HasOperator { get; }

    public bool HasModeSeparator { get; }

    // Every '$' operand that is one binding path token, with document spans.
    public IReadOnlyList<BindingPathSyntax> Sources { get; }

    public BindingExpressionShape Classify(bool booleanTarget, bool stringTarget)
    {
        if (Text.IndexOf('$') < 0 || (stringTarget && !Text.StartsWith("$", StringComparison.Ordinal)))
        {
            return BindingExpressionShape.None;
        }

        if (IsWholeBindingToken)
        {
            return BindingExpressionShape.None;
        }

        if (!Parsed)
        {
            return booleanTarget ? BindingExpressionShape.Rejected : BindingExpressionShape.None;
        }

        if (IsSingleSource)
        {
            return BindingExpressionShape.None;
        }

        if (!booleanTarget)
        {
            return HasOperator ? BindingExpressionShape.NonBooleanTarget : BindingExpressionShape.None;
        }

        return HasModeSeparator ? BindingExpressionShape.Rejected : BindingExpressionShape.Expression;
    }
}

// Tokenizes a property value with the @if/@when expression grammar used by the markup
// generator, so the editor recognizes exactly the values the generator treats as
// property expressions. Type rules stay in the generator.
internal static class BindingExpressionSyntaxParser
{
    public static BindingExpressionSyntax Parse(string rawText, int absoluteOffset = 0)
    {
        if (rawText is null)
        {
            throw new ArgumentNullException(nameof(rawText));
        }

        // The generator reads the entity-decoded attribute value; map decoded offsets back to the document.
        (string decoded, int[] map) = Decode(rawText);
        int leading = 0;
        while (leading < decoded.Length && char.IsWhiteSpace(decoded[leading]))
        {
            leading++;
        }

        int trailing = decoded.Length;
        while (trailing > leading && char.IsWhiteSpace(decoded[trailing - 1]))
        {
            trailing--;
        }

        string text = decoded.Substring(leading, trailing - leading);
        TextSpan span = new(absoluteOffset + map[leading], map[trailing] - map[leading]);
        List<ExpressionToken>? tokens = Lex(text);
        List<BindingPathSyntax> sources = new();
        bool parsed = false;
        bool isSingleSource = false;
        bool hasOperator = false;
        if (tokens is not null)
        {
            foreach (ExpressionToken token in tokens)
            {
                if (token.Kind != ExpressionTokenKind.Atom || token.Text.Length == 0 || token.Text[0] != '$')
                {
                    continue;
                }

                int rawStart = map[leading + token.Offset];
                int rawEnd = map[leading + token.Offset + token.Text.Length];
                if (rawEnd - rawStart != token.Text.Length)
                {
                    continue;
                }

                EmbeddedParseResult<BindingValueSyntax> path = BindingSyntaxParser.Parse(
                    token.Text,
                    absoluteOffset + rawStart);
                if (path.Syntax.Kind == BindingValueKind.Direct && path.Syntax.Binding is BindingPathSyntax binding)
                {
                    sources.Add(binding);
                }
            }

            ExpressionShapeParser parser = new(tokens);
            parsed = parser.TryParse(out isSingleSource, out hasOperator);
        }

        return new BindingExpressionSyntax(
            text,
            span,
            IsWholeBindingToken(text),
            parsed,
            isSingleSource,
            hasOperator,
            FindModeSeparator(text) >= 0,
            sources);
    }

    // True when the generator's leading binding token (a lexically valid path with an
    // optional ':' word) spans the whole value, as in "$DataContext.Name:TwoWay".
    private static bool IsWholeBindingToken(string text)
    {
        if (text.Length == 0 || text[0] != '$')
        {
            return false;
        }

        int index = 1;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] is '_' or '.' or '$'))
        {
            index++;
        }

        if (!IsLexicallyValidBindingPath(text.Substring(0, index)))
        {
            return false;
        }

        if (index < text.Length && text[index] == ':')
        {
            index++;
            while (index < text.Length && char.IsLetter(text[index]))
            {
                index++;
            }
        }

        return index == text.Length;
    }

    private static bool IsLexicallyValidBindingPath(string path)
    {
        if (path.Length < 2 || path[0] != '$' || path.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        string[] segments = path.Split('.');
        if (!IsBindingIdentifier(segments[0].Substring(1)))
        {
            return false;
        }

        for (int index = 1; index < segments.Length; index++)
        {
            string segment = segments[index];
            if (segment.StartsWith("$", StringComparison.Ordinal))
            {
                segment = segment.Substring(1);
            }

            if (!IsBindingIdentifier(segment))
            {
                return false;
            }
        }

        return segments[0] == "$DataContext" || segments.Length >= 2;
    }

    private static bool IsBindingIdentifier(string value)
    {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (!char.IsLetterOrDigit(value[index]) && value[index] != '_')
            {
                return false;
            }
        }

        return true;
    }

    // The offset of a ':' outside quoted string literals, or -1.
    private static int FindModeSeparator(string text)
    {
        bool quoted = false;
        bool escaped = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (quoted && character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character == ':')
            {
                return index;
            }
        }

        return -1;
    }

    private static (string Text, int[] Map) Decode(string raw)
    {
        System.Text.StringBuilder builder = new(raw.Length);
        List<int> map = new(raw.Length + 1);
        int index = 0;
        while (index < raw.Length)
        {
            if (raw[index] == '&')
            {
                int end = raw.IndexOf(';', index + 1);
                if (end > index + 1)
                {
                    string entity = raw.Substring(index, end - index + 1);
                    string decoded = WebUtility.HtmlDecode(entity);
                    if (!string.Equals(decoded, entity, StringComparison.Ordinal))
                    {
                        foreach (char character in decoded)
                        {
                            builder.Append(character);
                            map.Add(index);
                        }

                        index = end + 1;
                        continue;
                    }
                }
            }

            builder.Append(raw[index]);
            map.Add(index);
            index++;
        }

        map.Add(raw.Length);
        return (builder.ToString(), map.ToArray());
    }

    // The generator's lexer: parentheses, quoted strings, comparators, 'and', 'or' and
    // atoms. Returns null for an unterminated string literal.
    private static List<ExpressionToken>? Lex(string text)
    {
        List<ExpressionToken> result = new();
        int offset = 0;
        while (offset < text.Length)
        {
            if (char.IsWhiteSpace(text[offset]))
            {
                offset++;
                continue;
            }

            int start = offset;
            char character = text[offset];
            if (character == '(')
            {
                result.Add(new ExpressionToken(ExpressionTokenKind.OpenParenthesis, "(", start));
                offset++;
                continue;
            }

            if (character == ')')
            {
                result.Add(new ExpressionToken(ExpressionTokenKind.CloseParenthesis, ")", start));
                offset++;
                continue;
            }

            if (character == '"')
            {
                bool escaped = false;
                offset++;
                while (offset < text.Length)
                {
                    char quoted = text[offset++];
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (quoted == '\\')
                    {
                        escaped = true;
                    }
                    else if (quoted == '"')
                    {
                        break;
                    }
                }

                if (offset > text.Length || text[offset - 1] != '"')
                {
                    return null;
                }

                result.Add(new ExpressionToken(ExpressionTokenKind.Atom, text.Substring(start, offset - start), start));
                continue;
            }

            if (character is '<' or '>' or '=' or '!')
            {
                offset++;
                if (offset < text.Length && text[offset] == '=')
                {
                    offset++;
                }

                string comparator = text.Substring(start, offset - start);
                result.Add(new ExpressionToken(
                    comparator is "<" or "<=" or ">" or ">=" or "==" or "!="
                        ? ExpressionTokenKind.Comparator
                        : ExpressionTokenKind.Atom,
                    comparator,
                    start));
                continue;
            }

            while (offset < text.Length &&
                !char.IsWhiteSpace(text[offset]) &&
                text[offset] is not '(' and not ')' and not '<' and not '>' and not '=' and not '!')
            {
                offset++;
            }

            string atom = text.Substring(start, offset - start);
            result.Add(new ExpressionToken(
                atom switch
                {
                    "and" => ExpressionTokenKind.And,
                    "or" => ExpressionTokenKind.Or,
                    _ => ExpressionTokenKind.Atom
                },
                atom,
                start));
        }

        result.Add(new ExpressionToken(ExpressionTokenKind.End, string.Empty, text.Length));
        return result;
    }

    // The generator's recursive-descent grammar: or > and > comparison > primary.
    // Records only what the shape decision needs.
    private sealed class ExpressionShapeParser
    {
        private readonly List<ExpressionToken> tokens;
        private int index;

        public ExpressionShapeParser(List<ExpressionToken> tokens)
        {
            this.tokens = tokens;
        }

        private ExpressionToken Current => tokens[Math.Min(index, tokens.Count - 1)];

        public bool TryParse(out bool isSingleSource, out bool hasOperator)
        {
            isSingleSource = false;
            hasOperator = false;
            if (Current.Kind == ExpressionTokenKind.End)
            {
                return false;
            }

            Node? node = ParseOr();
            if (node is null || Current.Kind != ExpressionTokenKind.End)
            {
                return false;
            }

            isSingleSource = node.Kind == NodeKind.Source;
            hasOperator = node.HasOperator;
            return true;
        }

        private Node? ParseOr()
        {
            Node? left = ParseAnd();
            while (left is not null && Current.Kind == ExpressionTokenKind.Or)
            {
                index++;
                left = ParseAnd() is null ? null : Node.Operator;
            }

            return left;
        }

        private Node? ParseAnd()
        {
            Node? left = ParseComparison();
            while (left is not null && Current.Kind == ExpressionTokenKind.And)
            {
                index++;
                left = ParseComparison() is null ? null : Node.Operator;
            }

            return left;
        }

        private Node? ParseComparison()
        {
            Node? left = ParsePrimary();
            if (left is null || Current.Kind != ExpressionTokenKind.Comparator)
            {
                return left;
            }

            index++;
            return ParsePrimary() is null ? null : Node.Operator;
        }

        private Node? ParsePrimary()
        {
            ExpressionToken token = Current;
            if (token.Kind == ExpressionTokenKind.OpenParenthesis)
            {
                index++;
                Node? inner = ParseOr();
                if (inner is null || Current.Kind != ExpressionTokenKind.CloseParenthesis)
                {
                    return null;
                }

                index++;
                return Node.Group(inner.HasOperator);
            }

            if (token.Kind != ExpressionTokenKind.Atom)
            {
                return null;
            }

            index++;
            return string.Equals(token.Text, "value", StringComparison.Ordinal) || IsLiteral(token.Text)
                ? Node.Value
                : Node.Source;
        }

        private static bool IsLiteral(string text)
        {
            return text.Length > 0 && text[0] == '"' ||
                string.Equals(text, "Null", StringComparison.OrdinalIgnoreCase) ||
                bool.TryParse(text, out _) ||
                decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }

    private enum NodeKind
    {
        Source,
        Value,
        Group,
        Operator
    }

    private sealed class Node
    {
        public static readonly Node Source = new(NodeKind.Source, false);
        public static readonly Node Value = new(NodeKind.Value, false);
        public static readonly Node Operator = new(NodeKind.Operator, true);
        private static readonly Node GroupWithOperator = new(NodeKind.Group, true);
        private static readonly Node GroupWithoutOperator = new(NodeKind.Group, false);

        private Node(NodeKind kind, bool hasOperator)
        {
            Kind = kind;
            HasOperator = hasOperator;
        }

        public NodeKind Kind { get; }

        public bool HasOperator { get; }

        public static Node Group(bool hasOperator) => hasOperator ? GroupWithOperator : GroupWithoutOperator;
    }

    private enum ExpressionTokenKind
    {
        End,
        Atom,
        And,
        Or,
        OpenParenthesis,
        CloseParenthesis,
        Comparator
    }

    private readonly struct ExpressionToken
    {
        public ExpressionToken(ExpressionTokenKind kind, string text, int offset)
        {
            Kind = kind;
            Text = text;
            Offset = offset;
        }

        public ExpressionTokenKind Kind { get; }

        public string Text { get; }

        public int Offset { get; }
    }
}

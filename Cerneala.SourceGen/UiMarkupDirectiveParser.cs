using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    [Flags]
    private enum DirectiveContentKind
    {
        None = 0,
        Assignments = 1,
        Elements = 2,
        Templates = 4
    }

    private abstract class DirectiveNode
    {
        protected DirectiveNode(XObject source)
        {
            Source = source;
        }

        public XObject Source { get; }
    }

    private sealed class DirectiveElementNode : DirectiveNode
    {
        public DirectiveElementNode(XElement element) : base(element)
        {
            Element = element;
        }

        public XElement Element { get; }
    }

    private sealed class DirectiveTextNode : DirectiveNode
    {
        public DirectiveTextNode(string text, XObject source) : base(source)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveAssignmentNode : DirectiveNode
    {
        public DirectiveAssignmentNode(
            string propertyName,
            string value,
            XObject source,
            DirectiveExpressionLocation valueLocation) : base(source)
        {
            PropertyName = propertyName;
            Value = value;
            ValueLocation = valueLocation;
        }

        public string PropertyName { get; }

        public string Value { get; }

        public DirectiveExpressionLocation ValueLocation { get; }
    }

    private sealed class DirectiveDefaultNode : DirectiveNode
    {
        public DirectiveDefaultNode(IReadOnlyList<DirectiveNode> body, XObject source) : base(source)
        {
            Body = body;
        }

        public IReadOnlyList<DirectiveNode> Body { get; }
    }

    private sealed class DirectiveTemplateNode : DirectiveNode
    {
        public DirectiveTemplateNode(XElement root, XObject source) : base(source)
        {
            Root = root;
        }

        public XElement Root { get; }
    }

    private sealed class DirectiveExpressionLocation
    {
        public DirectiveExpressionLocation(XObject source, int offset)
        {
            Source = source;
            Offset = offset;
        }

        public XObject Source { get; }

        public int Offset { get; }
    }

    private abstract class DirectiveExpression
    {
        protected DirectiveExpression(DirectiveExpressionLocation location)
        {
            Location = location;
        }

        public DirectiveExpressionLocation Location { get; }
    }

    private sealed class DirectiveSourceExpression : DirectiveExpression
    {
        public DirectiveSourceExpression(string text, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveValueExpression : DirectiveExpression
    {
        public DirectiveValueExpression(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class DirectiveLiteralExpression : DirectiveExpression
    {
        public DirectiveLiteralExpression(string text, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveComparisonExpression : DirectiveExpression
    {
        public DirectiveComparisonExpression(
            DirectiveExpression left,
            string comparator,
            DirectiveExpression right,
            DirectiveExpressionLocation location) : base(location)
        {
            Left = left;
            Comparator = comparator;
            Right = right;
        }

        public DirectiveExpression Left { get; }

        public string Comparator { get; }

        public DirectiveExpression Right { get; }
    }

    private enum DirectiveLogicalOperator
    {
        And,
        Or
    }

    private sealed class DirectiveLogicalExpression : DirectiveExpression
    {
        public DirectiveLogicalExpression(
            DirectiveExpression left,
            DirectiveLogicalOperator @operator,
            DirectiveExpression right,
            DirectiveExpressionLocation location) : base(location)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public DirectiveExpression Left { get; }

        public DirectiveLogicalOperator Operator { get; }

        public DirectiveExpression Right { get; }
    }

    private sealed class DirectiveGroupExpression : DirectiveExpression
    {
        public DirectiveGroupExpression(DirectiveExpression inner, DirectiveExpressionLocation location) : base(location)
        {
            Inner = inner;
        }

        public DirectiveExpression Inner { get; }
    }

    private sealed class DirectiveWhenNode : DirectiveNode
    {
        public DirectiveWhenNode(
            DirectiveExpression expression,
            IReadOnlyList<DirectiveIfNode> branches,
            IReadOnlyList<DirectiveNode>? booleanBody,
            XObject source) : base(source)
        {
            Expression = expression;
            Branches = branches;
            BooleanBody = booleanBody;
        }

        public DirectiveExpression Expression { get; }

        public IReadOnlyList<DirectiveIfNode> Branches { get; }

        public IReadOnlyList<DirectiveNode>? BooleanBody { get; }
    }

    private sealed class DirectiveIfNode : DirectiveNode
    {
        public DirectiveIfNode(DirectiveExpression expression, IReadOnlyList<DirectiveNode> body, XObject source) : base(source)
        {
            Expression = expression;
            Body = body;
        }

        public DirectiveExpression Expression { get; }

        public IReadOnlyList<DirectiveNode> Body { get; }
    }

    private sealed class DirectiveParseResult
    {
        public DirectiveParseResult(IReadOnlyList<DirectiveNode> nodes, string? error, object? errorSource)
        {
            Nodes = nodes;
            Error = error;
            ErrorSource = errorSource;
        }

        public IReadOnlyList<DirectiveNode> Nodes { get; }

        public string? Error { get; }

        public object? ErrorSource { get; }

        public bool HasDirectives => Nodes.Any(ContainsDirective);

        private static bool ContainsDirective(DirectiveNode node)
        {
            return node is DirectiveWhenNode or DirectiveDefaultNode;
        }
    }

    private static DirectiveParseResult ParseDirectiveContent(XElement element, DirectiveContentKind allowedContent)
    {
        DirectiveCursor cursor = new(element.Nodes());
        try
        {
            IReadOnlyList<DirectiveNode> nodes = cursor.ParseNodes(stopAtClosingBrace: false, allowedContent);
            return new DirectiveParseResult(nodes, null, null);
        }
        catch (DirectiveParseException ex)
        {
            return new DirectiveParseResult([], ex.Message, ex.LocationSource ?? element);
        }
    }

    private sealed class DirectiveCursor
    {
        private readonly IReadOnlyList<Segment> segments;
        private int segmentIndex;
        private int characterIndex;

        public DirectiveCursor(IEnumerable<XNode> nodes)
        {
            segments = nodes.Select(node => node switch
            {
                XText text => new Segment(text.Value, null, text),
                XElement element => new Segment(null, element, element),
                _ => new Segment(string.Empty, null, node)
            }).ToArray();
        }

        public IReadOnlyList<DirectiveNode> ParseNodes(bool stopAtClosingBrace, DirectiveContentKind allowedContent)
        {
            List<DirectiveNode> nodes = [];
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    if (stopAtClosingBrace)
                    {
                        throw Error("Missing closing '}'.");
                    }

                    return nodes;
                }

                if (CurrentElement is XElement element)
                {
                    XObject source = CurrentSource;
                    AdvanceSegment();
                    if (!Allows(allowedContent, DirectiveContentKind.Elements))
                    {
                        throw new DirectiveParseException("XML controls are not allowed in this directive context.", source);
                    }

                    nodes.Add(new DirectiveElementNode(element));
                    continue;
                }

                if (Peek() == '}')
                {
                    if (!stopAtClosingBrace)
                    {
                        throw Error("Unexpected closing '}'.");
                    }

                    Read();
                    return nodes;
                }

                if (StartsWith("@when"))
                {
                    nodes.Add(ParseWhen(allowedContent));
                    continue;
                }

                if (StartsWith("@default"))
                {
                    nodes.Add(ParseDefault(allowedContent));
                    continue;
                }

                if (StartsWith("@template"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.Templates))
                    {
                        throw Error("@template is not allowed in this directive context.");
                    }

                    nodes.Add(ParseTemplate());
                    continue;
                }

                if (StartsWith("@if"))
                {
                    throw Error("@if must be declared directly inside an @when block.");
                }

                if (Peek() == '@')
                {
                    string directive = ReadWord();
                    throw Error("Unsupported directive '" + directive + "'.");
                }

                if (Allows(allowedContent, DirectiveContentKind.Assignments) && LooksLikeAssignment())
                {
                    nodes.Add(ParseAssignment());
                    continue;
                }

                DirectiveTextNode? text = ParseText(stopAtClosingBrace);
                if (text is not null)
                {
                    nodes.Add(text);
                }
            }
        }

        private DirectiveWhenNode ParseWhen(DirectiveContentKind allowedContent)
        {
            XObject source = CurrentSource;
            Consume("@when");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());

            SkipWhitespace();
            if (!StartsWith("@if"))
            {
                DirectiveContentKind booleanContent =
                    (allowedContent | DirectiveContentKind.Assignments) & ~DirectiveContentKind.Templates;
                IReadOnlyList<DirectiveNode> booleanBody = ParseNodes(stopAtClosingBrace: true, booleanContent);
                if (booleanBody.Count == 0)
                {
                    throw new DirectiveParseException("@when requires a boolean body or at least one @if block.", source);
                }

                return new DirectiveWhenNode(expression, [], booleanBody, source);
            }

            List<DirectiveIfNode> branches = [];
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for @when.", source);
                }

                if (Peek() == '}')
                {
                    Read();
                    break;
                }

                if (!StartsWith("@if"))
                {
                    throw Error("Only @if blocks may appear directly inside @when.");
                }

                // Assignments are valid inside @if even though bare assignments are
                // intentionally rejected at the surrounding XML element level.
                DirectiveContentKind branchContent =
                    (allowedContent | DirectiveContentKind.Assignments) & ~DirectiveContentKind.Templates;
                branches.Add(ParseIf(branchContent));
            }

            if (branches.Count == 0)
            {
                throw new DirectiveParseException("@when requires at least one @if block.", source);
            }

            return new DirectiveWhenNode(expression, branches, null, source);
        }

        private DirectiveIfNode ParseIf(DirectiveContentKind allowedContent)
        {
            XObject source = CurrentSource;
            Consume("@if");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());
            IReadOnlyList<DirectiveNode> body = ParseNodes(stopAtClosingBrace: true, allowedContent);
            return new DirectiveIfNode(expression, body, source);
        }

        private static DirectiveExpression ParseExpression(DirectiveHeader header)
        {
            return new DirectiveExpressionParser(header).Parse();
        }

        private DirectiveDefaultNode ParseDefault(DirectiveContentKind allowedContent)
        {
            XObject source = CurrentSource;
            Consume("@default");
            SkipWhitespace();
            if (Read() != '{')
            {
                throw new DirectiveParseException("@default must be followed by a block.", source);
            }

            return new DirectiveDefaultNode(
                ParseNodes(stopAtClosingBrace: true, allowedContent & ~DirectiveContentKind.Templates),
                source);
        }

        private DirectiveTemplateNode ParseTemplate()
        {
            XObject source = CurrentSource;
            Consume("@template");
            SkipWhitespace();
            if (Read() != '{')
            {
                throw new DirectiveParseException("@template must be followed by a block.", source);
            }

            IReadOnlyList<DirectiveNode> body = ParseNodes(
                stopAtClosingBrace: true,
                DirectiveContentKind.Elements);
            DirectiveElementNode[] roots = body.OfType<DirectiveElementNode>().ToArray();
            if (roots.Length != 1 || body.Count != 1)
            {
                throw new DirectiveParseException("@template requires exactly one XML root element.", source);
            }

            return new DirectiveTemplateNode(roots[0].Element, source);
        }

        private DirectiveAssignmentNode ParseAssignment()
        {
            XObject source = CurrentSource;
            string propertyName = ReadIdentifier();
            SkipWhitespace();
            if (Read() != '=')
            {
                throw new DirectiveParseException("Property assignment requires '='.", source);
            }

            SkipWhitespace();
            XObject valueSource = CurrentSource;
            int valueOffset = characterIndex;
            StringBuilder value = new();
            bool quoted = false;
            bool escaped = false;
            while (!AtEnd && CurrentElement is null)
            {
                char character = Read();
                if (escaped)
                {
                    value.Append(character);
                    escaped = false;
                    continue;
                }

                if (character == '\\' && quoted)
                {
                    value.Append(character);
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                    value.Append(character);
                    continue;
                }

                if (character == ';' && !quoted)
                {
                    string rawValue = value.ToString().Trim();
                    if (rawValue.Length == 0)
                    {
                        throw new DirectiveParseException("Property assignment requires a value.", source);
                    }

                    return new DirectiveAssignmentNode(
                        propertyName,
                        rawValue,
                        source,
                        new DirectiveExpressionLocation(valueSource, valueOffset));
                }

                value.Append(character);
            }

            throw new DirectiveParseException("Property assignment must end with ';'.", source);
        }

        private DirectiveTextNode? ParseText(bool stopAtClosingBrace)
        {
            XObject source = CurrentSource;
            StringBuilder builder = new();
            while (!AtEnd && CurrentElement is null)
            {
                if (Peek() == '@' || (stopAtClosingBrace && Peek() == '}'))
                {
                    break;
                }

                builder.Append(Read());
            }

            string text = builder.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : new DirectiveTextNode(text.Trim(), source);
        }

        private bool LooksLikeAssignment()
        {
            Position saved = Save();
            try
            {
                string identifier = ReadIdentifier();
                if (identifier.Length == 0)
                {
                    return false;
                }

                SkipWhitespace();
                return !AtEnd && CurrentElement is null && Peek() == '=';
            }
            finally
            {
                Restore(saved);
            }
        }

        private static bool Allows(DirectiveContentKind allowed, DirectiveContentKind value)
        {
            return (allowed & value) == value;
        }

        private DirectiveHeader ReadHeaderUntilBrace()
        {
            XObject source = CurrentSource;
            int offset = characterIndex;
            StringBuilder builder = new();
            bool quoted = false;
            bool escaped = false;
            while (!AtEnd && CurrentElement is null)
            {
                char character = Read();
                if (escaped)
                {
                    builder.Append(character);
                    escaped = false;
                    continue;
                }

                if (character == '\\' && quoted)
                {
                    builder.Append(character);
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                }

                if (character == '{' && !quoted)
                {
                    return new DirectiveHeader(builder.ToString(), source, offset);
                }

                builder.Append(character);
            }

            throw Error("Directive must be followed by a block.");
        }

        private string ReadWord()
        {
            StringBuilder builder = new();
            while (!AtEnd && CurrentElement is null && !char.IsWhiteSpace(Peek()) && Peek() != '{' && Peek() != '}')
            {
                builder.Append(Read());
            }

            return builder.ToString();
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            StringBuilder builder = new();
            while (!AtEnd && CurrentElement is null && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                builder.Append(Read());
            }

            return builder.ToString();
        }

        private void Consume(string value)
        {
            if (!StartsWith(value))
            {
                throw Error("Expected '" + value + "'.");
            }

            for (int index = 0; index < value.Length; index++)
            {
                Read();
            }
        }

        private bool StartsWith(string value)
        {
            Position saved = Save();
            try
            {
                foreach (char expected in value)
                {
                    if (AtEnd || CurrentElement is not null || Read() != expected)
                    {
                        return false;
                    }
                }

                return AtEnd || CurrentElement is not null || !char.IsLetterOrDigit(Peek());
            }
            finally
            {
                Restore(saved);
            }
        }

        private void SkipWhitespace()
        {
            while (!AtEnd && CurrentElement is null && char.IsWhiteSpace(Peek()))
            {
                Read();
            }
        }

        private char Peek()
        {
            Normalize();
            return CurrentText![characterIndex];
        }

        private char Read()
        {
            char value = Peek();
            characterIndex++;
            Normalize();
            return value;
        }

        private void AdvanceSegment()
        {
            segmentIndex++;
            characterIndex = 0;
            Normalize();
        }

        private void Normalize()
        {
            while (segmentIndex < segments.Count &&
                segments[segmentIndex].Element is null &&
                characterIndex >= (segments[segmentIndex].Text?.Length ?? 0))
            {
                segmentIndex++;
                characterIndex = 0;
            }
        }

        private Position Save()
        {
            return new Position(segmentIndex, characterIndex);
        }

        private void Restore(Position position)
        {
            segmentIndex = position.Segment;
            characterIndex = position.Character;
        }

        private DirectiveParseException Error(string message)
        {
            return new DirectiveParseException(message, AtEnd ? segments.LastOrDefault()?.Source : CurrentSource);
        }

        private bool AtEnd
        {
            get
            {
                Normalize();
                return segmentIndex >= segments.Count;
            }
        }

        private string? CurrentText => AtEnd ? null : segments[segmentIndex].Text;

        private XElement? CurrentElement => AtEnd ? null : segments[segmentIndex].Element;

        private XObject CurrentSource => segments[segmentIndex].Source;

        private sealed class Segment
        {
            public Segment(string? text, XElement? element, XObject source)
            {
                Text = text;
                Element = element;
                Source = source;
            }

            public string? Text { get; }

            public XElement? Element { get; }

            public XObject Source { get; }
        }

        private sealed class DirectiveHeader
        {
            public DirectiveHeader(string text, XObject source, int offset)
            {
                Text = text;
                Source = source;
                Offset = offset;
            }

            public string Text { get; }

            public XObject Source { get; }

            public int Offset { get; }
        }

        private sealed class DirectiveExpressionParser
        {
            private readonly DirectiveHeader header;
            private readonly IReadOnlyList<ExpressionToken> tokens;
            private int index;

            public DirectiveExpressionParser(DirectiveHeader header)
            {
                this.header = header;
                tokens = Lex(header);
            }

            public DirectiveExpression Parse()
            {
                if (Current.Kind == ExpressionTokenKind.End)
                {
                    throw Error(Current, "Directive expression is empty.");
                }

                DirectiveExpression expression = ParseOr();
                if (Current.Kind == ExpressionTokenKind.CloseParenthesis)
                {
                    throw Error(Current, "Unexpected closing parenthesis ')'.");
                }

                if (Current.Kind != ExpressionTokenKind.End)
                {
                    throw Error(Current, "Expected logical operator 'and' or 'or'.");
                }

                return expression;
            }

            private DirectiveExpression ParseOr()
            {
                DirectiveExpression left = ParseAnd();
                while (Current.Kind == ExpressionTokenKind.Or)
                {
                    ExpressionToken token = Read();
                    DirectiveExpression right = ParseAnd();
                    left = new DirectiveLogicalExpression(left, DirectiveLogicalOperator.Or, right, Location(token));
                }

                return left;
            }

            private DirectiveExpression ParseAnd()
            {
                DirectiveExpression left = ParseComparison();
                while (Current.Kind == ExpressionTokenKind.And)
                {
                    ExpressionToken token = Read();
                    DirectiveExpression right = ParseComparison();
                    left = new DirectiveLogicalExpression(left, DirectiveLogicalOperator.And, right, Location(token));
                }

                return left;
            }

            private DirectiveExpression ParseComparison()
            {
                DirectiveExpression left = ParsePrimary();
                if (Current.Kind != ExpressionTokenKind.Comparator)
                {
                    return left;
                }

                ExpressionToken comparator = Read();
                DirectiveExpression right = ParsePrimary();
                return new DirectiveComparisonExpression(left, comparator.Text, right, Location(comparator));
            }

            private DirectiveExpression ParsePrimary()
            {
                ExpressionToken token = Current;
                if (token.Kind == ExpressionTokenKind.OpenParenthesis)
                {
                    Read();
                    DirectiveExpression inner = ParseOr();
                    if (Current.Kind != ExpressionTokenKind.CloseParenthesis)
                    {
                        throw Error(Current, "Missing closing parenthesis ')'.");
                    }

                    Read();
                    return new DirectiveGroupExpression(inner, Location(token));
                }

                if (token.Kind != ExpressionTokenKind.Atom)
                {
                    throw Error(token, "Expected expression operand.");
                }

                Read();
                DirectiveExpressionLocation location = Location(token);
                if (string.Equals(token.Text, "value", StringComparison.Ordinal))
                {
                    return new DirectiveValueExpression(location);
                }

                if (IsLiteral(token.Text))
                {
                    return new DirectiveLiteralExpression(token.Text, location);
                }

                return new DirectiveSourceExpression(token.Text, location);
            }

            private ExpressionToken Read()
            {
                return tokens[index++];
            }

            private ExpressionToken Current => tokens[Math.Min(index, tokens.Count - 1)];

            private DirectiveExpressionLocation Location(ExpressionToken token)
            {
                return new DirectiveExpressionLocation(header.Source, header.Offset + token.Offset);
            }

            private DirectiveParseException Error(ExpressionToken token, string message)
            {
                return new DirectiveParseException(message, Location(token));
            }

            private static bool IsLiteral(string text)
            {
                return text.Length > 0 && text[0] == '"' ||
                    string.Equals(text, "Null", StringComparison.OrdinalIgnoreCase) ||
                    bool.TryParse(text, out _) ||
                    decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            }

            private static IReadOnlyList<ExpressionToken> Lex(DirectiveHeader header)
            {
                List<ExpressionToken> result = [];
                string text = header.Text;
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
                            throw new DirectiveParseException(
                                "String literal is missing its closing quote.",
                                new DirectiveExpressionLocation(header.Source, header.Offset + start));
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
                        if (comparator is "<" or "<=" or ">" or ">=" or "==" or "!=")
                        {
                            result.Add(new ExpressionToken(ExpressionTokenKind.Comparator, comparator, start));
                        }
                        else
                        {
                            result.Add(new ExpressionToken(ExpressionTokenKind.Atom, comparator, start));
                        }

                        continue;
                    }

                    while (offset < text.Length &&
                        !char.IsWhiteSpace(text[offset]) &&
                        text[offset] is not '(' and not ')' and not '<' and not '>' and not '=' and not '!')
                    {
                        offset++;
                    }

                    string atom = text.Substring(start, offset - start);
                    ExpressionTokenKind kind = atom switch
                    {
                        "and" => ExpressionTokenKind.And,
                        "or" => ExpressionTokenKind.Or,
                        _ => ExpressionTokenKind.Atom
                    };
                    result.Add(new ExpressionToken(kind, atom, start));
                }

                result.Add(new ExpressionToken(ExpressionTokenKind.End, string.Empty, text.Length));
                return result;
            }
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

        private readonly struct Position
        {
            public Position(int segment, int character)
            {
                Segment = segment;
                Character = character;
            }

            public int Segment { get; }

            public int Character { get; }
        }
    }

    private sealed class DirectiveParseException : Exception
    {
        public DirectiveParseException(string message, object? locationSource) : base(message)
        {
            LocationSource = locationSource;
        }

        public object? LocationSource { get; }
    }
}

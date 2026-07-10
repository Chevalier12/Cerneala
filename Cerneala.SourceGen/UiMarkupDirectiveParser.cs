using System;
using System.Collections.Generic;
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
        public DirectiveAssignmentNode(string propertyName, string value, XObject source) : base(source)
        {
            PropertyName = propertyName;
            Value = value;
        }

        public string PropertyName { get; }

        public string Value { get; }
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

    private sealed class DirectiveWhenNode : DirectiveNode
    {
        public DirectiveWhenNode(
            string sourceExpression,
            IReadOnlyList<DirectiveIfNode> branches,
            IReadOnlyList<DirectiveNode>? booleanBody,
            XObject source) : base(source)
        {
            SourceExpression = sourceExpression;
            Branches = branches;
            BooleanBody = booleanBody;
        }

        public string SourceExpression { get; }

        public IReadOnlyList<DirectiveIfNode> Branches { get; }

        public IReadOnlyList<DirectiveNode>? BooleanBody { get; }
    }

    private sealed class DirectiveIfNode : DirectiveNode
    {
        public DirectiveIfNode(string comparator, string operand, IReadOnlyList<DirectiveNode> body, XObject source) : base(source)
        {
            Comparator = comparator;
            Operand = operand;
            Body = body;
        }

        public string Comparator { get; }

        public string Operand { get; }

        public IReadOnlyList<DirectiveNode> Body { get; }
    }

    private sealed class DirectiveParseResult
    {
        public DirectiveParseResult(IReadOnlyList<DirectiveNode> nodes, string? error, XObject? errorSource)
        {
            Nodes = nodes;
            Error = error;
            ErrorSource = errorSource;
        }

        public IReadOnlyList<DirectiveNode> Nodes { get; }

        public string? Error { get; }

        public XObject? ErrorSource { get; }

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
            return new DirectiveParseResult([], ex.Message, ex.Source ?? element);
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
            string sourceExpression = ReadHeaderUntilBrace().Trim();
            if (sourceExpression.Length == 0 || sourceExpression.Any(char.IsWhiteSpace))
            {
                throw new DirectiveParseException("@when requires exactly one source expression.", source);
            }

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

                return new DirectiveWhenNode(sourceExpression, [], booleanBody, source);
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

            return new DirectiveWhenNode(sourceExpression, branches, null, source);
        }

        private DirectiveIfNode ParseIf(DirectiveContentKind allowedContent)
        {
            XObject source = CurrentSource;
            Consume("@if");
            string header = ReadHeaderUntilBrace().Trim();
            if (!header.StartsWith("value", StringComparison.Ordinal) ||
                (header.Length > 5 && !char.IsWhiteSpace(header[5])))
            {
                throw new DirectiveParseException("@if condition must start with 'value'.", source);
            }

            string comparison = header.Substring(5).Trim();
            string? comparator = new[] { "<=", ">=", "==", "!=", "<", ">" }
                .FirstOrDefault(candidate => comparison.StartsWith(candidate, StringComparison.Ordinal));
            if (comparator is null)
            {
                throw new DirectiveParseException("@if requires one of ==, !=, <, <=, >, >=.", source);
            }

            string operand = comparison.Substring(comparator.Length).Trim();
            if (operand.Length == 0)
            {
                throw new DirectiveParseException("@if requires a comparison operand.", source);
            }

            IReadOnlyList<DirectiveNode> body = ParseNodes(stopAtClosingBrace: true, allowedContent);
            return new DirectiveIfNode(comparator, operand, body, source);
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

                    return new DirectiveAssignmentNode(propertyName, rawValue, source);
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

        private string ReadHeaderUntilBrace()
        {
            StringBuilder builder = new();
            bool quoted = false;
            while (!AtEnd && CurrentElement is null)
            {
                char character = Read();
                if (character == '"')
                {
                    quoted = !quoted;
                }

                if (character == '{' && !quoted)
                {
                    return builder.ToString();
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
        public DirectiveParseException(string message, XObject? source) : base(message)
        {
            Source = source;
        }

        public new XObject? Source { get; }
    }
}

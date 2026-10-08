using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private static DirectiveParseResult ParseDirectiveContent(MarkupElement element, DirectiveContentKind allowedContent)
    {
        DirectiveCursor cursor = new(element.Nodes());
        try
        {
            IReadOnlyList<DirectiveNode> nodes = cursor.ParseNodes(stopAtClosingBrace: false, allowedContent);
            return new DirectiveParseResult(nodes, null, null);
        }
        catch (PrismSyntaxParseException ex)
        {
            return new DirectiveParseResult(
                [],
                null,
                null,
                [new PrismSyntaxDiagnostic(ex.Descriptor, ex.Message, ex.LocationSource ?? element)]);
        }
        catch (DirectiveParseException ex)
        {
            return new DirectiveParseResult([], ex.Message, ex.LocationSource ?? element);
        }
    }

    private sealed partial class DirectiveCursor
    {
        private readonly IReadOnlyList<Segment> segments;
        private int segmentIndex;
        private int characterIndex;

        public DirectiveCursor(IEnumerable<MarkupNode> nodes)
        {
            segments = nodes.Select(node => node switch
            {
                MarkupText text => new Segment(text.Value, null, text),
                MarkupElement element => new Segment(null, element, element),
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

                if (CurrentElement is MarkupElement element)
                {
                    MarkupObject source = CurrentSource;
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

                if (StartsWith("@prism"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.Prism))
                    {
                        throw Error("@prism is not allowed in this directive context.");
                    }

                    nodes.Add(ParsePrismApplication());
                    continue;
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

                if (StartsWith("@templates"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.Templates))
                    {
                        throw Error("@templates is not allowed in this directive context.");
                    }

                    nodes.Add(ParseTemplates());
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

                if (StartsWith("@on"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionTriggers))
                    {
                        throw Error("@on is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseOn());
                    continue;
                }

                if (StartsWith("@presence"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionPresence))
                    {
                        throw Error("@presence is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParsePresence());
                    continue;
                }

                if (StartsWith("@layout"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionLayout))
                    {
                        throw Error("@layout is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseLayout());
                    continue;
                }

                if (StartsWith("@scroll"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionScroll))
                    {
                        throw Error("@scroll is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseScroll());
                    continue;
                }

                if (StartsWith("@drag"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionDrag))
                    {
                        throw Error("@drag is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseDrag());
                    continue;
                }

                if (StartsWith("@gesture"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionGesture))
                    {
                        throw Error("@gesture is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseGesture());
                    continue;
                }

                if (StartsWith("@animate"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@animate is allowed only inside an Aspect @when, @if or @on block.");
                    }

                    nodes.Add(ParseAnimate());
                    continue;
                }

                if (StartsWith("@set"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@set is allowed only inside an Aspect execution body.");
                    }

                    nodes.Add(ParseMotionSet());
                    continue;
                }

                if (StartsWith("@keyframes"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@keyframes is allowed only inside an Aspect execution body.");
                    }

                    nodes.Add(ParseKeyframes());
                    continue;
                }

                if (StartsWith("@stagger"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@stagger is allowed only inside an Aspect execution body.");
                    }

                    nodes.Add(ParseStagger());
                    continue;
                }

                if (StartsWith("@parallel"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@parallel is allowed only inside an Aspect @when, @if or @on block.");
                    }

                    nodes.Add(ParseMotionComposition("@parallel", MotionCompositionKind.Parallel));
                    continue;
                }

                if (StartsWith("@sequence"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@sequence is allowed only inside an Aspect @when, @if or @on block.");
                    }

                    nodes.Add(ParseMotionComposition("@sequence", MotionCompositionKind.Sequence));
                    continue;
                }

                if (StartsWith("@run"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@run is allowed only inside an Aspect execution body.");
                    }

                    nodes.Add(ParseMotionRun());
                    continue;
                }

                if (TryParseTimbreDirective(allowedContent, out DirectiveNode? timbreAction))
                {
                    nodes.Add(timbreAction!);
                    continue;
                }

                if (StartsWith("@cancel"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionExecutions))
                    {
                        throw Error("@cancel is allowed only inside an Aspect execution body.");
                    }

                    nodes.Add(ParseMotionCancel());
                    continue;
                }

                if (StartsWith("@handle"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionHandles))
                    {
                        throw Error("@handle is allowed only directly inside an Aspect body.");
                    }

                    nodes.Add(ParseMotionHandle());
                    continue;
                }

                if (StartsWith("@parameter"))
                {
                    if (!Allows(allowedContent, DirectiveContentKind.MotionParameters))
                    {
                        throw Error("@parameter is allowed only at the beginning of a MotionClip.");
                    }

                    nodes.Add(ParseMotionParameter());
                    continue;
                }

                if (StartsWith("@from") || StartsWith("@to"))
                {
                    throw Error("@from and @to are allowed only directly inside an @animate block.");
                }

                if (StartsWith("@if"))
                {
                    throw Error("@if must be declared directly inside an @when block.");
                }

                if (Peek() == '@')
                {
                    string directive = ReadWord();
                    if (IsMotionDirective(directive))
                    {
                        throw Error(directive + " is allowed only inside an Aspect body.");
                    }

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
            MarkupObject source = CurrentSource;
            Consume("@when");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());

            SkipWhitespace();
            if (!StartsWith("@if"))
            {
                DirectiveContentKind booleanContent =
                    (allowedContent | DirectiveContentKind.Assignments | DirectiveContentKind.MotionExecutions | TimbreContent(allowedContent)) &
                    ~(DirectiveContentKind.Templates | DirectiveContentKind.MotionTriggers | DirectiveContentKind.MotionHandles |
                        DirectiveContentKind.MotionPresence | DirectiveContentKind.MotionLayout);
                IReadOnlyList<DirectiveNode> booleanBody = ParseNodes(stopAtClosingBrace: true, booleanContent);
                if (booleanBody.Count == 0)
                {
                    throw new DirectiveParseException("@when requires a boolean body or at least one @if block.", source);
                }

                ValidateExplicitMotionComposition(booleanBody, "@when", source);

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



                DirectiveContentKind branchContent =
                    (allowedContent | DirectiveContentKind.Assignments | DirectiveContentKind.MotionExecutions | TimbreContent(allowedContent)) &
                    ~(DirectiveContentKind.Templates | DirectiveContentKind.MotionTriggers | DirectiveContentKind.MotionHandles |
                        DirectiveContentKind.MotionPresence | DirectiveContentKind.MotionLayout);
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
            MarkupObject source = CurrentSource;
            Consume("@if");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());
            IReadOnlyList<DirectiveNode> body = ParseNodes(stopAtClosingBrace: true, allowedContent);
            ValidateExplicitMotionComposition(body, "@if", source);
            return new DirectiveIfNode(expression, body, source);
        }

        private DirectiveDefaultNode ParseDefault(DirectiveContentKind allowedContent)
        {
            MarkupObject source = CurrentSource;
            Consume("@default");
            SkipWhitespace();
            if (Read() != '{')
            {
                throw new DirectiveParseException("@default must be followed by a block.", source);
            }

            return new DirectiveDefaultNode(
                ParseNodes(
                    stopAtClosingBrace: true,
                    allowedContent & ~(DirectiveContentKind.Templates | DirectiveContentKind.MotionHandles |
                        DirectiveContentKind.MotionPresence)),
                source);
        }

        private DirectiveTemplateNode ParseTemplate()
        {
            MarkupObject source = CurrentSource;
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

        private DirectiveTemplatesNode ParseTemplates()
        {
            MarkupObject source = CurrentSource;
            Consume("@templates");
            SkipWhitespace();
            if (Read() != '{')
            {
                throw new DirectiveParseException("@templates must be followed by a block.", source);
            }

            IReadOnlyList<DirectiveNode> body = ParseNodes(
                stopAtClosingBrace: true,
                DirectiveContentKind.Elements);
            DirectiveElementNode[] templates = body.OfType<DirectiveElementNode>().ToArray();
            if (templates.Length == 0 || templates.Length != body.Count)
            {
                throw new DirectiveParseException("@templates requires one or more XML elements.", source);
            }

            return new DirectiveTemplatesNode(templates.Select(node => node.Element).ToArray(), source);
        }

        private DirectiveAssignmentNode ParseAssignment()
        {
            SkipWhitespace();
            MarkupObject source = CurrentSource;
            int propertyOffset = characterIndex;
            string propertyName = ReadIdentifier();
            SkipWhitespace();
            if (Read() != '=')
            {
                throw new DirectiveParseException("Property assignment requires '='.", source);
            }

            SkipWhitespace();
            MarkupObject valueSource = CurrentSource;
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
                        new DirectiveExpressionLocation(source, propertyOffset, Math.Max(1, propertyName.Length)),
                        new DirectiveExpressionLocation(valueSource, valueOffset));
                }

                value.Append(character);
            }

            if (quoted)
            {
                throw new DirectiveParseException("String literal is missing its closing quote.", valueSource);
            }

            throw new DirectiveParseException("Property assignment must end with ';'.", source);
        }

        private DirectiveTextNode? ParseText(bool stopAtClosingBrace)
        {
            MarkupObject source = CurrentSource;
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

        private static int FindTopLevelKeyword(string text, string keyword)
        {
            int parentheses = 0;
            bool quoted = false;
            for (int index = 0; index <= text.Length - keyword.Length; index++)
            {
                char character = text[index];
                if (character == '"' && (index == 0 || text[index - 1] != '\\'))
                {
                    quoted = !quoted;
                }
                else if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                }

                if (!quoted && parentheses == 0 &&
                    string.CompareOrdinal(text, index, keyword, 0, keyword.Length) == 0 &&
                    (index == 0 || char.IsWhiteSpace(text[index - 1])) &&
                    (index + keyword.Length == text.Length || char.IsWhiteSpace(text[index + keyword.Length])))
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindTopLevelCharacter(string text, char expected, int start = 0)
        {
            int parentheses = 0;
            bool quoted = false;
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (character == '"' && (index == 0 || text[index - 1] != '\\'))
                {
                    quoted = !quoted;
                }
                else if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                }
                else if (index >= start && !quoted && parentheses == 0 && character == expected)
                {
                    return index;
                }
            }

            return -1;
        }

        private static IReadOnlyList<SplitPart> SplitTopLevel(string text, char separator)
        {
            List<SplitPart> parts = [];
            int start = 0;
            int parentheses = 0;
            bool quoted = false;
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (character == '"' && (index == 0 || text[index - 1] != '\\'))
                {
                    quoted = !quoted;
                }
                else if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                }
                else if (!quoted && parentheses == 0 && character == separator)
                {
                    parts.Add(new SplitPart(text.Substring(start, index - start), start));
                    start = index + 1;
                }
            }

            if (text.Length > 0)
            {
                parts.Add(new SplitPart(text.Substring(start), start));
            }

            return parts;
        }

        private static bool IsIdentifier(string text)
        {
            return text.Length > 0 &&
                (char.IsLetter(text[0]) || text[0] == '_') &&
                text.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
        }

        private static bool IsMotionDirective(string directive)
        {
            return MotionMarkupLanguage.IsDirective(directive);
        }

        private static bool Allows(DirectiveContentKind allowed, DirectiveContentKind value)
        {
            return (allowed & value) == value;
        }

        private DirectiveHeader ReadHeaderUntilBrace()
        {
            MarkupObject source = CurrentSource;
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

                return AtEnd ||
                    CurrentElement is not null ||
                    !char.IsLetterOrDigit(Peek()) && Peek() != '_';
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

        private MarkupElement? CurrentElement => AtEnd ? null : segments[segmentIndex].Element;

        private MarkupObject CurrentSource => segments[segmentIndex].Source;

        private sealed class Segment
        {
            public Segment(string? text, MarkupElement? element, MarkupObject source)
            {
                Text = text;
                Element = element;
                Source = source;
            }

            public string? Text { get; }

            public MarkupElement? Element { get; }

            public MarkupObject Source { get; }
        }

        private sealed class DirectiveHeader
        {
            public DirectiveHeader(string text, MarkupObject source, int offset)
            {
                Text = text;
                Source = source;
                Offset = offset;
            }

            public string Text { get; }

            public MarkupObject Source { get; }

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

        private readonly struct SplitPart
        {
            public SplitPart(string text, int offset)
            {
                Text = text;
                Offset = offset;
            }

            public string Text { get; }

            public int Offset { get; }
        }
    }

}

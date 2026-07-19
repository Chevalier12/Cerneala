using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        Templates = 4,
        MotionTriggers = 8,
        MotionExecutions = 16,
        MotionParameters = 32,
        MotionHandles = 64,
        MotionPresence = 128,
        MotionLayout = 256,
        MotionScroll = 512,
        MotionDrag = 1024,
        MotionGesture = 2048,
        Prism = 4096
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
            DirectiveExpressionLocation propertyLocation,
            DirectiveExpressionLocation valueLocation) : base(source)
        {
            PropertyName = propertyName;
            Value = value;
            PropertyLocation = propertyLocation;
            ValueLocation = valueLocation;
        }

        public string PropertyName { get; }

        public string Value { get; }

        public DirectiveExpressionLocation PropertyLocation { get; }

        public DirectiveExpressionLocation ValueLocation { get; }
    }

    private sealed class DirectivePrismNode : DirectiveNode
    {
        public DirectivePrismNode(PrismApplicationSyntax application, XObject source) : base(source)
        {
            Application = application;
        }

        public PrismApplicationSyntax Application { get; }
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
        public DirectiveExpressionLocation(XObject source, int offset, int length = 1)
        {
            Source = source;
            Offset = offset;
            Length = length;
        }

        public XObject Source { get; }

        public int Offset { get; }

        public int Length { get; }
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
        public DirectiveParseResult(
            IReadOnlyList<DirectiveNode> nodes,
            string? error,
            object? errorSource,
            IReadOnlyList<PrismSyntaxDiagnostic>? prismDiagnostics = null)
        {
            Nodes = nodes;
            Error = error;
            ErrorSource = errorSource;
            PrismDiagnostics = prismDiagnostics ?? [];
        }

        public IReadOnlyList<DirectiveNode> Nodes { get; }

        public string? Error { get; }

        public object? ErrorSource { get; }

        public IReadOnlyList<PrismSyntaxDiagnostic> PrismDiagnostics { get; }

        public bool HasDirectives => Nodes.Any(ContainsDirective);

        private static bool ContainsDirective(DirectiveNode node)
        {
            return node is DirectiveWhenNode or DirectiveDefaultNode or DirectiveOnNode;
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
            XObject source = CurrentSource;
            Consume("@when");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());

            SkipWhitespace();
            if (!StartsWith("@if"))
            {
                DirectiveContentKind booleanContent =
                    (allowedContent | DirectiveContentKind.Assignments | DirectiveContentKind.MotionExecutions) &
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

                // Assignments are valid inside @if even though bare assignments are
                // intentionally rejected at the surrounding XML element level.
                DirectiveContentKind branchContent =
                    (allowedContent | DirectiveContentKind.Assignments | DirectiveContentKind.MotionExecutions) &
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
            XObject source = CurrentSource;
            Consume("@if");
            DirectiveExpression expression = ParseExpression(ReadHeaderUntilBrace());
            IReadOnlyList<DirectiveNode> body = ParseNodes(stopAtClosingBrace: true, allowedContent);
            ValidateExplicitMotionComposition(body, "@if", source);
            return new DirectiveIfNode(expression, body, source);
        }

        private static DirectiveExpression ParseExpression(DirectiveHeader header)
        {
            return new DirectiveExpressionParser(header).Parse();
        }

        private DirectiveOnNode ParseOn()
        {
            XObject source = CurrentSource;
            Consume("@on");
            DirectiveHeader header = ReadHeaderUntilBrace();
            string eventName = header.Text.Trim();
            if (!IsIdentifier(eventName))
            {
                throw new DirectiveParseException("@on requires one event name.", new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            IReadOnlyList<DirectiveNode> nodes = ParseNodes(
                stopAtClosingBrace: true,
                DirectiveContentKind.MotionExecutions);
            if (nodes.Count == 0 || nodes.Any(node => node is not MotionExecutionNode))
            {
                throw new DirectiveParseException("@on requires one Motion execution body.", source);
            }

            ValidateExplicitMotionComposition(nodes, "@on", source);

            int eventOffset = header.Text.IndexOf(eventName, StringComparison.Ordinal);
            return new DirectiveOnNode(
                eventName,
                nodes.Cast<MotionExecutionNode>().ToArray(),
                new DirectiveExpressionLocation(header.Source, header.Offset + Math.Max(0, eventOffset), eventName.Length),
                source);
        }

        private MotionPresenceNode ParsePresence()
        {
            XObject source = CurrentSource;
            Consume("@presence");
            DirectiveHeader header = ReadHeaderUntilBrace();
            if (!string.IsNullOrWhiteSpace(header.Text))
            {
                throw new DirectiveParseException("@presence does not accept a header or custom endpoints.", new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            MotionSpecSyntax? enter = null;
            MotionSpecSyntax? exit = null;
            bool excludeInputWhileExiting = true;
            HashSet<string> seen = new(StringComparer.Ordinal);
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for @presence.", source);
                }

                if (CurrentElement is not null)
                {
                    throw new DirectiveParseException("XML controls and custom bodies are not allowed inside @presence.", CurrentSource);
                }

                if (Peek() == '}')
                {
                    Read();
                    break;
                }

                if (Peek() == '@')
                {
                    throw Error("Custom @enter and @exit bodies are not supported by @presence.");
                }

                DirectiveAssignmentNode assignment = ParseAssignment();
                if (!seen.Add(assignment.PropertyName))
                {
                    throw new DirectiveParseException("Duplicate @presence field '" + assignment.PropertyName + "'.", assignment.Source);
                }

                switch (assignment.PropertyName)
                {
                    case "enter":
                        enter = ParseMotionSpec(assignment.Value, assignment.ValueLocation);
                        break;
                    case "exit":
                        exit = ParseMotionSpec(assignment.Value, assignment.ValueLocation);
                        break;
                    case "excludeInputWhileExiting":
                        if (!bool.TryParse(assignment.Value, out excludeInputWhileExiting))
                        {
                            throw new DirectiveParseException("@presence field 'excludeInputWhileExiting' requires true or false.", assignment.ValueLocation);
                        }

                        break;
                    default:
                        throw new DirectiveParseException(
                            "Unsupported @presence field '" + assignment.PropertyName + "'. Custom endpoints and initial mode are not supported.",
                            assignment.Source);
                }
            }

            if (enter is null || exit is null)
            {
                throw new DirectiveParseException("@presence requires both enter and exit Motion specs.", source);
            }

            return new MotionPresenceNode(enter, exit, excludeInputWhileExiting, source);
        }

        private MotionLayoutNode ParseLayout()
        {
            XObject source = CurrentSource;
            Consume("@layout");
            SkipWhitespace();
            int statementOffset = characterIndex;
            string statement = ReadMotionStatement().Trim();
            if (!statement.StartsWith("id ", StringComparison.Ordinal))
            {
                throw new DirectiveParseException("@layout requires 'id expression with MotionSpec'.", source);
            }

            string body = statement.Substring(3).Trim();
            int separator = body.LastIndexOf(" with ", StringComparison.Ordinal);
            if (separator <= 0 || separator + 6 >= body.Length)
            {
                throw new DirectiveParseException("@layout requires 'id expression with MotionSpec'.", source);
            }

            string idText = body.Substring(0, separator).Trim();
            string specText = body.Substring(separator + 6).Trim();
            if (idText.IndexOfAny(new[] { '{', '}', ',' }) >= 0 ||
                specText.IndexOfAny(new[] { '{', '}' }) >= 0 ||
                body.IndexOf(" mode ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                body.IndexOf("crossfade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                body.IndexOf("shared", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new DirectiveParseException("@layout supports only retained-element render correction; modes, crossfade, shared elements and custom sequences are not supported.", source);
            }

            DirectiveExpressionLocation idLocation = new(source, statementOffset + statement.IndexOf(idText, StringComparison.Ordinal));
            DirectiveExpression expression = ParseExpression(new DirectiveHeader(idText, source, idLocation.Offset));
            if (expression is not DirectiveSourceExpression id)
            {
                throw new DirectiveParseException("@layout id requires one reactive source expression.", idLocation);
            }

            DirectiveExpressionLocation specLocation = new(source, statementOffset + statement.LastIndexOf(specText, StringComparison.Ordinal));
            return new MotionLayoutNode(id, ParseMotionSpec(specText, specLocation), source);
        }

        private MotionScrollNode ParseScroll()
        {
            XObject source = CurrentSource;
            Consume("@scroll");
            DirectiveHeader header = ReadHeaderUntilBrace();
            Match match = Regex.Match(
                header.Text,
                @"^\s*source\s+(\$[A-Za-z_][A-Za-z0-9_]*)\s+axis\s+(vertical|horizontal)(?:\s+allowLayout\s*=\s*(true|false))?\s*$",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!match.Success || string.Equals(match.Groups[1].Value, "$part", StringComparison.Ordinal))
            {
                throw new DirectiveParseException(
                    "@scroll requires 'source $Name axis vertical|horizontal' and optionally 'allowLayout = true'. Pixel ranges, easing, input subranges and keyframes are not supported.",
                    new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            MotionScrollAxis axis = string.Equals(match.Groups[2].Value, "vertical", StringComparison.OrdinalIgnoreCase)
                ? MotionScrollAxis.Vertical
                : MotionScrollAxis.Horizontal;
            bool allowLayout = match.Groups[3].Success && bool.Parse(match.Groups[3].Value);
            List<MotionScrollAssignmentSyntax> assignments = [];
            HashSet<string> targets = new(StringComparer.Ordinal);
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for @scroll.", source);
                }

                if (CurrentElement is not null || Peek() == '@')
                {
                    throw new DirectiveParseException("@scroll accepts only linear float range assignments.", CurrentSource);
                }

                if (Peek() == '}')
                {
                    Read();
                    break;
                }

                DirectiveAssignmentNode assignment = ParseAssignment();
                int separator = assignment.Value.IndexOf("..", StringComparison.Ordinal);
                if (separator <= 0 || separator != assignment.Value.LastIndexOf("..", StringComparison.Ordinal) ||
                    !float.TryParse(assignment.Value.Substring(0, separator).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float from) ||
                    !float.TryParse(assignment.Value.Substring(separator + 2).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float to) ||
                    float.IsNaN(from) || float.IsInfinity(from) || float.IsNaN(to) || float.IsInfinity(to))
                {
                    throw new DirectiveParseException(
                        "@scroll assignments require one finite float output range 'from..to'; pixels, easing, input subranges and keyframes are not supported.",
                        assignment.ValueLocation);
                }

                if (!targets.Add(assignment.PropertyName))
                {
                    throw new DirectiveParseException("Duplicate @scroll target '" + assignment.PropertyName + "'.", assignment.Source);
                }

                assignments.Add(new MotionScrollAssignmentSyntax(assignment.PropertyName, from, to, assignment.ValueLocation));
            }

            if (assignments.Count == 0)
            {
                throw new DirectiveParseException("@scroll requires at least one float range assignment.", source);
            }

            return new MotionScrollNode(match.Groups[1].Value, axis, allowLayout, assignments, source);
        }

        private MotionDragNode ParseDrag()
        {
            XObject source = CurrentSource;
            Consume("@drag");
            SkipWhitespace();
            int statementOffset = characterIndex;
            string statement = ReadMotionStatement().Trim();
            if (!statement.StartsWith("with ", StringComparison.Ordinal) || statement.Length == 5)
            {
                throw new DirectiveParseException(
                    "@drag requires exactly 'with MotionSpec'. Axis, source, target, bounds, resistance and snapping options are not supported.",
                    source);
            }

            string specText = statement.Substring(5).Trim();
            if (specText.IndexOfAny(new[] { '{', '}' }) >= 0 ||
                specText.IndexOf(" axis ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" source ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" target ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" bounds ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" resistance ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" snapping ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new DirectiveParseException(
                    "@drag supports only one release MotionSpec; axis, source, target, bounds, resistance and snapping options are not supported.",
                    source);
            }

            DirectiveExpressionLocation location = new(source, statementOffset + statement.LastIndexOf(specText, StringComparison.Ordinal));
            MotionSpecSyntax spec = ParseMotionSpec(specText, location);
            if (spec is MotionInlineSpecSyntax { Kind: "Decay" })
            {
                throw new DirectiveParseException("@drag does not support a Decay release spec.", location);
            }

            return new MotionDragNode(spec, source);
        }

        private MotionGesturePressNode ParseGesture()
        {
            XObject source = CurrentSource;
            Consume("@gesture");
            SkipWhitespace();
            int statementOffset = characterIndex;
            string statement = ReadMotionStatement().Trim();
            const string prefix = "press with ";
            if (!statement.StartsWith(prefix, StringComparison.Ordinal) || statement.Length == prefix.Length)
            {
                throw new DirectiveParseException(
                    "@gesture supports exactly 'press with MotionSpec'. Pinch, rotate and custom scale endpoints are not supported.",
                    source);
            }

            string specText = statement.Substring(prefix.Length).Trim();
            if (specText.IndexOfAny(new[] { '{', '}' }) >= 0 ||
                specText.IndexOf(" scale ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" from ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                specText.IndexOf(" to ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new DirectiveParseException(
                    "@gesture press uses the fixed runtime endpoints 0.97 and 1; custom scale endpoints are not supported.",
                    source);
            }

            DirectiveExpressionLocation location = new(source, statementOffset + statement.LastIndexOf(specText, StringComparison.Ordinal));
            return new MotionGesturePressNode(ParseMotionSpec(specText, location), source);
        }

        private MotionCompositionNode ParseMotionComposition(string directive, MotionCompositionKind kind)
        {
            XObject source = CurrentSource;
            Consume(directive);
            DirectiveHeader header = ReadHeaderUntilBrace();
            if (!string.IsNullOrWhiteSpace(header.Text))
            {
                throw new DirectiveParseException(directive + " does not accept a header.", new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            IReadOnlyList<DirectiveNode> nodes = ParseNodes(
                stopAtClosingBrace: true,
                DirectiveContentKind.MotionExecutions);
            if (nodes.Count == 0)
            {
                throw new DirectiveParseException(directive + " requires at least one child execution body.", source);
            }

            if (nodes.Any(node => node is not MotionExecutionNode))
            {
                throw new DirectiveParseException(directive + " accepts only Motion execution bodies.", source);
            }

            if (nodes.Any(node => node is MotionCancelNode))
            {
                throw new DirectiveParseException(directive + " cannot contain @cancel.", source);
            }

            return new MotionCompositionNode(kind, nodes.Cast<MotionExecutionNode>().ToArray(), source);
        }

        private MotionRunNode ParseMotionRun()
        {
            XObject source = CurrentSource;
            Consume("@run");
            SkipWhitespace();
            int statementOffset = characterIndex;
            string statement = ReadMotionStatement().Trim();
            string? handleName = null;
            int handleSeparator = statement.LastIndexOf(" as ", StringComparison.Ordinal);
            if (handleSeparator >= 0)
            {
                handleName = statement.Substring(handleSeparator + 4).Trim();
                statement = statement.Substring(0, handleSeparator).Trim();
                if (!IsIdentifier(handleName))
                {
                    throw new DirectiveParseException("@run 'as' requires a declared handle name.", source);
                }
            }

            if (statement.Length < 2 || statement[0] != '$')
            {
                throw new DirectiveParseException("@run requires one MotionClip resource reference such as '$Clip'.", source);
            }

            int open = statement.IndexOf('(');
            string clipName = open < 0 ? statement.Substring(1) : statement.Substring(1, open - 1).Trim();
            if (!IsIdentifier(clipName) ||
                (open >= 0 && (statement[statement.Length - 1] != ')' || open == statement.Length - 1)))
            {
                throw new DirectiveParseException("@run requires '$Clip' followed by optional named arguments.", source);
            }

            List<MotionRunArgumentSyntax> arguments = [];
            if (open >= 0)
            {
                string argumentsText = statement.Substring(open + 1, statement.Length - open - 2);
                foreach (SplitPart part in SplitTopLevel(argumentsText, ','))
                {
                    string argument = part.Text.Trim();
                    int equals = FindTopLevelCharacter(argument, '=');
                    string name = equals < 0 ? string.Empty : argument.Substring(0, equals).Trim();
                    string value = equals < 0 ? string.Empty : argument.Substring(equals + 1).Trim();
                    DirectiveExpressionLocation location = new(source, statementOffset + open + 1 + part.Offset);
                    if (!IsIdentifier(name) || value.Length == 0)
                    {
                        throw new DirectiveParseException("@run arguments must use 'Name = value'.", location);
                    }

                    arguments.Add(new MotionRunArgumentSyntax(name, value, location));
                }
            }

            return new MotionRunNode(clipName, arguments, handleName, source);
        }

        private MotionCancelNode ParseMotionCancel()
        {
            XObject source = CurrentSource;
            Consume("@cancel");
            SkipWhitespace();
            string handleName = ReadMotionStatement().Trim();
            if (!IsIdentifier(handleName))
            {
                throw new DirectiveParseException("@cancel requires one declared handle name.", source);
            }

            return new MotionCancelNode(handleName, source);
        }

        private MotionHandleNode ParseMotionHandle()
        {
            XObject source = CurrentSource;
            Consume("@handle");
            SkipWhitespace();
            string name = ReadMotionStatement().Trim();
            if (!IsIdentifier(name))
            {
                throw new DirectiveParseException("@handle requires one name.", source);
            }

            return new MotionHandleNode(name, source);
        }

        private MotionParameterNode ParseMotionParameter()
        {
            XObject source = CurrentSource;
            Consume("@parameter");
            SkipWhitespace();
            int statementOffset = characterIndex;
            string statement = ReadMotionStatement().Trim();
            int colon = statement.IndexOf(':');
            int equals = FindTopLevelCharacter(statement, '=');
            string name = colon < 0 ? string.Empty : statement.Substring(0, colon).Trim();
            string typeName = colon < 0
                ? string.Empty
                : statement.Substring(colon + 1, (equals < 0 ? statement.Length : equals) - colon - 1).Trim();
            string? defaultValue = equals < 0 ? null : statement.Substring(equals + 1).Trim();
            DirectiveExpressionLocation location = new(source, statementOffset);
            if (!IsIdentifier(name) || typeName.Length == 0 || defaultValue is not null && defaultValue.Length == 0)
            {
                throw new DirectiveParseException("@parameter requires 'Name: Type' and an optional '= default'.", location);
            }

            return new MotionParameterNode(name, typeName, defaultValue, location, source);
        }

        private static void ValidateExplicitMotionComposition(
            IReadOnlyList<DirectiveNode> nodes,
            string owner,
            XObject source)
        {
            if (nodes.OfType<MotionExecutionNode>().Skip(1).Any())
            {
                throw new DirectiveParseException(
                    owner + " contains sibling Motion executions; wrap them in @parallel or @sequence.",
                    source);
            }
        }

        private MotionAnimateNode ParseAnimate()
        {
            XObject source = CurrentSource;
            Consume("@animate");
            DirectiveHeader header = ReadHeaderUntilBrace();
            if (string.Equals(header.Text.Trim(), "hold", StringComparison.Ordinal))
            {
                throw new DirectiveParseException("hold and Step(...) are allowed only inside @keyframes.", source);
            }

            MotionSpecSyntax? defaultSpec = ParseAnimateSpec(header);
            if (defaultSpec is MotionInlineSpecSyntax inline && inline.Kind == "Step")
            {
                throw new DirectiveParseException("hold and Step(...) are allowed only inside @keyframes.", source);
            }

            return ParseAnimateBody(source, defaultSpec);
        }

        private MotionSetNode ParseMotionSet()
        {
            XObject source = CurrentSource;
            IReadOnlyList<MotionAssignmentSyntax> assignments = ParseMotionAssignmentBlock("@set");
            MotionAssignmentSyntax? invalid = assignments.FirstOrDefault(assignment =>
                assignment.Spec is not null || assignment.Value is MotionCurrentValueSyntax);
            if (invalid is not null)
            {
                throw new DirectiveParseException(
                    "@set accepts concrete values without 'current' or a Motion spec.",
                    invalid.Location);
            }

            return new MotionSetNode(assignments, source);
        }

        private MotionAnimateNode ParseAnimateBody(XObject source, MotionSpecSyntax? defaultSpec)
        {
            List<MotionOptionSyntax> options = [];
            IReadOnlyList<MotionAssignmentSyntax>? from = null;
            IReadOnlyList<MotionAssignmentSyntax>? to = null;

            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for @animate.", source);
                }

                if (CurrentElement is not null)
                {
                    throw new DirectiveParseException("XML controls are not allowed in Motion execution bodies.", CurrentSource);
                }

                if (Peek() == '}')
                {
                    Read();
                    break;
                }

                if (StartsWith("@from"))
                {
                    if (from is not null)
                    {
                        throw Error("@animate may contain only one @from block.");
                    }

                    from = ParseMotionAssignmentBlock("@from");
                    continue;
                }

                if (StartsWith("@to"))
                {
                    if (to is not null)
                    {
                        throw Error("@animate may contain only one @to block.");
                    }

                    to = ParseMotionAssignmentBlock("@to");
                    continue;
                }

                if (Peek() == '@')
                {
                    string directive = ReadWord();
                    throw Error("Unsupported directive '" + directive + "' inside @animate.");
                }

                DirectiveAssignmentNode option = ParseAssignment();
                options.Add(new MotionOptionSyntax(
                    option.PropertyName,
                    ParseMotionValue(option.Value, option.ValueLocation),
                    option.ValueLocation));
            }

            if (to is null)
            {
                throw new DirectiveParseException("@animate requires an @to block.", source);
            }

            return new MotionAnimateNode(defaultSpec, options, from ?? [], to, source);
        }

        private MotionKeyframesNode ParseKeyframes()
        {
            XObject source = CurrentSource;
            Consume("@keyframes");
            DirectiveHeader header = ReadHeaderUntilBrace();
            string text = header.Text.Trim();
            if (!text.StartsWith("duration", StringComparison.Ordinal) ||
                (text.Length > 8 && !char.IsWhiteSpace(text[8])))
            {
                throw new DirectiveParseException(
                    "@keyframes requires 'duration <positive duration>'.",
                    new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            string durationText = text.Substring(8).Trim();
            MotionDurationSyntax? duration = TryParseDuration(
                durationText,
                new DirectiveExpressionLocation(header.Source, header.Offset + header.Text.IndexOf(durationText, StringComparison.Ordinal)));
            if (duration is null || duration.Value <= 0)
            {
                throw new DirectiveParseException("@keyframes duration must be positive.", source);
            }

            List<MotionKeyframeSegmentSyntax> segments = [];
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for @keyframes.", source);
                }

                if (Peek() == '}')
                {
                    Read();
                    break;
                }

                if (!StartsWith("@animate"))
                {
                    throw Error("@keyframes accepts only ranged @animate children; nested groups are not allowed.");
                }

                XObject animationSource = CurrentSource;
                Consume("@animate");
                DirectiveHeader animationHeader = ReadHeaderUntilBrace();
                ParseKeyframeRangeHeader(animationHeader, out float start, out float end, out bool hold, out MotionSpecSyntax? spec);
                segments.Add(new MotionKeyframeSegmentSyntax(start, end, hold, ParseAnimateBody(animationSource, spec)));
            }

            if (segments.Count == 0)
            {
                throw new DirectiveParseException("@keyframes requires at least one ranged @animate child.", source);
            }

            return new MotionKeyframesNode(duration, segments, source);
        }

        private MotionStaggerNode ParseStagger()
        {
            XObject source = CurrentSource;
            Consume("@stagger");
            DirectiveHeader header = ReadHeaderUntilBrace();
            string[] parts = header.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || parts[0] != "target" || parts[2] != "each" ||
                parts[1].Length < 2 || parts[1][0] != '$' || !IsIdentifier(parts[1].Substring(1)) ||
                string.Equals(parts[1], "$part", StringComparison.Ordinal))
            {
                throw new DirectiveParseException("@stagger requires 'target $Name each <duration>'.", source);
            }

            MotionDurationSyntax? each = TryParseDuration(parts[3], new DirectiveExpressionLocation(header.Source, header.Offset));
            if (each is null || each.Value < 0)
            {
                throw new DirectiveParseException("@stagger each duration must be non-negative.", source);
            }

            SkipWhitespace();
            if (!StartsWith("@animate"))
            {
                throw new DirectiveParseException("@stagger requires exactly one Tween @animate child.", source);
            }

            XObject animationSource = CurrentSource;
            Consume("@animate");
            DirectiveHeader animationHeader = ReadHeaderUntilBrace();
            MotionSpecSyntax? spec = ParseAnimateSpec(animationHeader);
            if (spec is not MotionResourceSpecSyntax && spec is not MotionInlineSpecSyntax { Kind: "Tween" })
            {
                throw new DirectiveParseException("@stagger requires exactly one Tween @animate child.", animationSource);
            }

            MotionAnimateNode animation = ParseAnimateBody(animationSource, spec);
            SkipWhitespace();
            if (AtEnd || Peek() != '}')
            {
                throw new DirectiveParseException("@stagger requires exactly one Tween @animate child.", source);
            }

            Read();
            return new MotionStaggerNode(parts[1].Substring(1), each, animation, source);
        }

        private static void ParseKeyframeRangeHeader(
            DirectiveHeader header,
            out float start,
            out float end,
            out bool hold,
            out MotionSpecSyntax? spec)
        {
            string text = header.Text.Trim();
            int with = FindTopLevelKeyword(text, "with");
            string rangeAndHold = (with < 0 ? text : text.Substring(0, with)).Trim();
            string[] headerParts = rangeAndHold.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            hold = headerParts.Length == 2 && string.Equals(headerParts[1], "hold", StringComparison.Ordinal);
            if (headerParts.Length is < 1 or > 2 || headerParts.Length == 2 && !hold)
            {
                throw new DirectiveParseException("Ranged @animate accepts only an optional 'hold' before 'with'.", header.Source);
            }

            string range = headerParts[0];
            string[] bounds = range.Split(new[] { ".." }, StringSplitOptions.None);
            if (bounds.Length != 2 || !TryParsePercentage(bounds[0], out start) || !TryParsePercentage(bounds[1], out end))
            {
                throw new DirectiveParseException(
                    "Ranged @animate requires 'start%..end%'.",
                    new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            if (start < 0 || end > 1)
            {
                throw new DirectiveParseException("Keyframe range must be contained in 0%..100%.", header.Source);
            }

            if (start > end)
            {
                throw new DirectiveParseException("Keyframe range must be ordered.", header.Source);
            }

            if (start == end)
            {
                throw new DirectiveParseException("Keyframe range must be non-empty.", header.Source);
            }

            if (with < 0)
            {
                spec = null;
                return;
            }

            string specText = text.Substring(with + 4).Trim();
            spec = ParseMotionSpec(
                specText,
                new DirectiveExpressionLocation(header.Source, header.Offset + with + 4, specText.Length));
        }

        private static bool TryParsePercentage(string text, out float value)
        {
            text = text.Trim();
            if (!text.EndsWith("%", StringComparison.Ordinal) ||
                !float.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float percentage) ||
                float.IsNaN(percentage) || float.IsInfinity(percentage))
            {
                value = 0;
                return false;
            }

            value = percentage / 100f;
            return true;
        }

        private IReadOnlyList<MotionAssignmentSyntax> ParseMotionAssignmentBlock(string directive)
        {
            Consume(directive);
            SkipWhitespace();
            XObject source = CurrentSource;
            if (AtEnd || CurrentElement is not null || Read() != '{')
            {
                throw new DirectiveParseException(directive + " must be followed by a block.", source);
            }

            List<MotionAssignmentSyntax> assignments = [];
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw new DirectiveParseException("Missing closing '}' for " + directive + ".", source);
                }

                if (CurrentElement is not null)
                {
                    throw new DirectiveParseException("XML controls are not allowed in Motion assignment blocks.", CurrentSource);
                }

                if (Peek() == '}')
                {
                    Read();
                    if (assignments.Count == 0)
                    {
                        throw new DirectiveParseException(directive + " requires at least one property assignment.", source);
                    }

                    return assignments;
                }

                XObject assignmentSource = CurrentSource;
                int assignmentOffset = characterIndex;
                string target = ReadMotionTarget();
                SkipWhitespace();
                if (target.Length == 0 || AtEnd || CurrentElement is not null || Read() != '=')
                {
                    throw new DirectiveParseException("Motion property assignment requires '='.", assignmentSource);
                }

                SkipWhitespace();
                XObject valueSource = CurrentSource;
                int valueOffset = characterIndex;
                string statement = ReadMotionStatement();
                DirectiveExpressionLocation valueLocation = new(valueSource, valueOffset, Math.Max(1, statement.Length));
                SplitMotionValueAndSpec(statement, valueLocation, out string valueText, out MotionSpecSyntax? spec);
                assignments.Add(new MotionAssignmentSyntax(
                    target,
                    ParseMotionValue(valueText, valueLocation),
                    spec,
                    new DirectiveExpressionLocation(assignmentSource, assignmentOffset, target.Length)));
            }
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
                ParseNodes(
                    stopAtClosingBrace: true,
                    allowedContent & ~(DirectiveContentKind.Templates | DirectiveContentKind.MotionHandles |
                        DirectiveContentKind.MotionPresence)),
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
            SkipWhitespace();
            XObject source = CurrentSource;
            int propertyOffset = characterIndex;
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

        private MotionSpecSyntax? ParseAnimateSpec(DirectiveHeader header)
        {
            string text = header.Text.Trim();
            if (text.Length == 0)
            {
                return null;
            }

            if (!text.StartsWith("with", StringComparison.Ordinal) ||
                (text.Length > 4 && !char.IsWhiteSpace(text[4])))
            {
                throw new DirectiveParseException(
                    "@animate accepts only an optional 'with' Motion spec before its block.",
                    new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            string spec = text.Substring(4).Trim();
            int specOffset = header.Text.IndexOf(spec, StringComparison.Ordinal);
            return ParseMotionSpec(
                spec,
                new DirectiveExpressionLocation(header.Source, header.Offset + Math.Max(0, specOffset), spec.Length));
        }

        private string ReadMotionTarget()
        {
            StringBuilder builder = new();
            while (!AtEnd && CurrentElement is null)
            {
                char character = Peek();
                if (char.IsLetterOrDigit(character) || character is '_' or '$' or '.')
                {
                    builder.Append(Read());
                    continue;
                }

                break;
            }

            string target = builder.ToString();
            if (IsIdentifier(target))
            {
                return target;
            }

            string[] parts = target.Split('.');
            bool hasValidOwner = parts[0].Length > 1 &&
                parts[0][0] == '$' &&
                !string.Equals(parts[0], "$part", StringComparison.Ordinal) &&
                IsIdentifier(parts[0].Substring(1));
            if (parts.Length == 2 && hasValidOwner && IsIdentifier(parts[1]))
            {
                return target;
            }

            if (parts.Length >= 4 &&
                hasValidOwner &&
                parts[1] == "prism" &&
                parts.Skip(2).All(IsIdentifier))
            {
                return target;
            }

            if (parts.Length == 4 &&
                hasValidOwner &&
                parts[1] == "parts" &&
                parts[2].Length > 1 &&
                parts[2][0] == '$' &&
                IsIdentifier(parts[2].Substring(1)) &&
                IsIdentifier(parts[3]))
            {
                return target;
            }

            throw Error(
                "Motion target must be Property, $self.Property, $owner.Property, " +
                "$Name.Property, $target.prism.Node.Property, or " +
                "$control.parts.$part.Property.");
        }

        private string ReadMotionStatement()
        {
            XObject source = CurrentSource;
            StringBuilder builder = new();
            int parentheses = 0;
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
                    builder.Append(character);
                    continue;
                }

                if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                    if (parentheses < 0)
                    {
                        throw new DirectiveParseException("Unexpected closing parenthesis ')' in Motion assignment.", source);
                    }
                }

                if (character == ';' && !quoted && parentheses == 0)
                {
                    string statement = builder.ToString().Trim();
                    if (statement.Length == 0)
                    {
                        throw new DirectiveParseException("Motion property assignment requires a value.", source);
                    }

                    return statement;
                }

                if (character is '{' or '}' && !quoted && parentheses == 0)
                {
                    throw new DirectiveParseException("Motion property assignment must end with ';'.", source);
                }

                builder.Append(character);
            }

            if (quoted)
            {
                throw new DirectiveParseException("String literal is missing its closing quote.", source);
            }

            if (parentheses != 0)
            {
                throw new DirectiveParseException("Motion expression has unbalanced parentheses.", source);
            }

            throw new DirectiveParseException("Motion property assignment must end with ';'.", source);
        }

        private static void SplitMotionValueAndSpec(
            string statement,
            DirectiveExpressionLocation location,
            out string value,
            out MotionSpecSyntax? spec)
        {
            int with = FindTopLevelKeyword(statement, "with");
            if (with < 0)
            {
                value = statement.Trim();
                spec = null;
                return;
            }

            value = statement.Substring(0, with).Trim();
            string specText = statement.Substring(with + 4).Trim();
            if (value.Length == 0 || specText.Length == 0)
            {
                throw new DirectiveParseException("A per-property 'with' requires both a value and a Motion spec.", location);
            }

            spec = ParseMotionSpec(
                specText,
                new DirectiveExpressionLocation(location.Source, location.Offset + with + 4, specText.Length));
        }

        private static MotionValueSyntax ParseMotionValue(string text, DirectiveExpressionLocation location)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                throw new DirectiveParseException("Motion value is empty.", location);
            }

            int question = FindTopLevelCharacter(text, '?');
            if (question >= 0)
            {
                int colon = FindTopLevelCharacter(text, ':', question + 1);
                if (colon < 0)
                {
                    throw new DirectiveParseException("Conditional Motion value requires ':'.", location);
                }

                string conditionText = text.Substring(0, question).Trim();
                string trueText = text.Substring(question + 1, colon - question - 1).Trim();
                string falseText = text.Substring(colon + 1).Trim();
                DirectiveExpression condition = ParseExpression(new DirectiveHeader(conditionText, location.Source, location.Offset));
                return new MotionConditionalValueSyntax(
                    condition,
                    ParseMotionValue(trueText, new DirectiveExpressionLocation(location.Source, location.Offset + question + 1)),
                    ParseMotionValue(falseText, new DirectiveExpressionLocation(location.Source, location.Offset + colon + 1)),
                    location);
            }

            if (string.Equals(text, "current", StringComparison.Ordinal))
            {
                return new MotionCurrentValueSyntax(location);
            }

            if (text[0] == '"')
            {
                if (text.Length < 2 || text[text.Length - 1] != '"')
                {
                    throw new DirectiveParseException("String literal is missing its closing quote.", location);
                }

                return new MotionAtomValueSyntax(text, location);
            }

            if (text.IndexOfAny([';', '{', '}']) >= 0 ||
                (text.Contains('(') || text.Contains(')')))
            {
                throw new DirectiveParseException("Motion values do not accept arbitrary expressions.", location);
            }

            return new MotionAtomValueSyntax(text, location);
        }

        public static MotionSpecSyntax ParseMotionSpec(string text, DirectiveExpressionLocation location)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                throw new DirectiveParseException("Motion spec is empty.", location);
            }

            if (text[0] == '$')
            {
                string name = text.Substring(1);
                if (!IsIdentifier(name))
                {
                    throw new DirectiveParseException("Motion resource reference must be '$Name'.", location);
                }

                return new MotionResourceSpecSyntax(name, location);
            }

            if (IsIdentifier(text))
            {
                return new MotionParameterSpecSyntax(text, location);
            }

            int open = text.IndexOf('(');
            if (open <= 0 || text[text.Length - 1] != ')')
            {
                throw new DirectiveParseException("Motion spec must be a resource reference or Tween(...)/Spring(...).", location);
            }

            string kind = text.Substring(0, open).Trim();
            if (kind is not "Tween" and not "Spring" and not "Step" and not "Repeat" and not "PingPong")
            {
                throw new DirectiveParseException("Unsupported inline Motion spec '" + kind + "'.", location);
            }

            string argumentsText = text.Substring(open + 1, text.Length - open - 2);
            IReadOnlyList<SplitPart> parts = SplitTopLevel(argumentsText, ',');
            if (parts.Count == 0 || parts.Any(part => part.Text.Trim().Length == 0))
            {
                throw new DirectiveParseException(kind + " requires arguments.", location);
            }

            List<MotionSpecArgumentSyntax> arguments = [];
            foreach (SplitPart part in parts)
            {
                string argument = part.Text.Trim();
                DirectiveExpressionLocation argumentLocation = new(location.Source, location.Offset + open + 1 + part.Offset);
                MotionDurationSyntax? duration = TryParseDuration(argument, argumentLocation);
                if (argument.EndsWith("ms", StringComparison.Ordinal) || argument.EndsWith("s", StringComparison.Ordinal))
                {
                    if (duration is null)
                    {
                        throw new DirectiveParseException("Invalid Motion duration '" + argument + "'. Use a number followed by ms or s.", argumentLocation);
                    }
                }

                if (argument.IndexOfAny(['{', '}', ';']) >= 0)
                {
                    throw new DirectiveParseException("Invalid token in inline Motion spec.", argumentLocation);
                }

                arguments.Add(new MotionSpecArgumentSyntax(argument, duration, argumentLocation));
            }

            if (kind is "Repeat" or "PingPong")
            {
                if (arguments.Count != 2 ||
                    !IsInlineTweenSpec(arguments[0]))
                {
                    throw new DirectiveParseException(kind + " requires Tween(...) as its first argument.", location);
                }

                bool validCount = kind == "Repeat" && arguments[1].Text == "forever" ||
                    int.TryParse(arguments[1].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) && count > 0;
                if (!validCount || kind == "PingPong" && arguments[1].Text == "forever")
                {
                    throw new DirectiveParseException(kind + " requires a positive finite count" + (kind == "Repeat" ? " or forever" : string.Empty) + ".", arguments[1].Location);
                }
            }

            if (kind == "Tween")
            {
                if (arguments.Count is < 1 or > 2 || arguments[0].Duration is null)
                {
                    throw new DirectiveParseException("Tween requires Tween(duration, easing?) with an ms or s duration.", location);
                }

                if (arguments.Count == 2 && !IsIdentifier(arguments[1].Text))
                {
                    throw new DirectiveParseException("Tween easing must be a named easing.", arguments[1].Location);
                }
            }
            else if (kind == "Step")
            {
                if (arguments.Count is < 1 or > 2 ||
                    !int.TryParse(arguments[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int steps) ||
                    steps <= 0 ||
                    arguments.Count == 2 && arguments[1].Text is not "JumpStart" and not "JumpEnd" and not "JumpBoth" and not "JumpNone" ||
                    arguments.Count == 2 && arguments[1].Text == "JumpNone" && steps < 2)
                {
                    throw new DirectiveParseException("Step requires a positive count and optional JumpStart, JumpEnd, JumpBoth or JumpNone.", location);
                }
            }

            return new MotionInlineSpecSyntax(kind, arguments, location);
        }

        private static bool IsInlineTweenSpec(MotionSpecArgumentSyntax argument)
        {
            try
            {
                return ParseMotionSpec(argument.Text, argument.Location) is MotionInlineSpecSyntax { Kind: "Tween" };
            }
            catch (DirectiveParseException)
            {
                return false;
            }
        }

        private static MotionDurationSyntax? TryParseDuration(string text, DirectiveExpressionLocation location)
        {
            string unit;
            string number;
            if (text.EndsWith("ms", StringComparison.Ordinal))
            {
                unit = "ms";
                number = text.Substring(0, text.Length - 2);
            }
            else if (text.EndsWith("s", StringComparison.Ordinal))
            {
                unit = "s";
                number = text.Substring(0, text.Length - 1);
            }
            else
            {
                return null;
            }

            return decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value) && value >= 0
                ? new MotionDurationSyntax(value, unit, location)
                : null;
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
                return new DirectiveExpressionLocation(
                    header.Source,
                    header.Offset + token.Offset,
                    Math.Max(1, token.Text.Length));
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

    private sealed class DirectiveParseException : Exception
    {
        public DirectiveParseException(string message, object? locationSource) : base(message)
        {
            LocationSource = locationSource;
        }

        public object? LocationSource { get; }
    }
}

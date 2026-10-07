using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class DirectiveCursor
    {
        private DirectiveOnNode ParseOn()
        {
            MarkupObject source = CurrentSource;
            Consume("@on");
            DirectiveHeader header = ReadHeaderUntilBrace();
            string eventName = header.Text.Trim();
            if (!IsIdentifier(eventName))
            {
                throw new DirectiveParseException("@on requires one event name.", new DirectiveExpressionLocation(header.Source, header.Offset));
            }

            IReadOnlyList<DirectiveNode> nodes = ParseNodes(
                stopAtClosingBrace: true,
                DirectiveContentKind.MotionExecutions | DirectiveContentKind.SoundActions);
            if (nodes.Count == 0 || nodes.Any(node => node is not MotionExecutionNode and not SoundActionNode))
            {
                throw new DirectiveParseException("@on requires one Motion execution body.", source);
            }

            ValidateExplicitMotionComposition(nodes, "@on", source);

            int eventOffset = header.Text.IndexOf(eventName, StringComparison.Ordinal);
            return new DirectiveOnNode(
                eventName,
                nodes.OfType<MotionExecutionNode>().ToArray(),
                nodes,
                new DirectiveExpressionLocation(header.Source, header.Offset + Math.Max(0, eventOffset), eventName.Length),
                source);
        }

        private MotionPresenceNode ParsePresence()
        {
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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

    }
}

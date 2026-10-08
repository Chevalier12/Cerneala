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
        private MotionCompositionNode ParseMotionComposition(string directive, MotionCompositionKind kind)
        {
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
            Consume("@cancel");
            SkipWhitespace();
            string handleName = ReadMotionStatement().Trim();
            if (!IsIdentifier(handleName))
            {
                throw new DirectiveParseException("@cancel requires one declared handle name.", source);
            }

            return new MotionCancelNode(handleName, source, actionSequence++);
        }

        private MotionHandleNode ParseMotionHandle()
        {
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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
            MarkupObject source)
        {
            // A body with Timbre actions may also @cancel Timbre handles; those
            // cancels are not Motion executions.
            bool hasTimbre = nodes.Any(node => node is TimbreActionNode);
            if (nodes.OfType<MotionExecutionNode>().Where(node => !hasTimbre || node is not MotionCancelNode).Skip(1).Any())
            {
                throw new DirectiveParseException(
                    owner + " contains sibling Motion executions; wrap them in @parallel or @sequence.",
                    source);
            }
        }

        private MotionAnimateNode ParseAnimate()
        {
            MarkupObject source = CurrentSource;
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
            MarkupObject source = CurrentSource;
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

        private MotionAnimateNode ParseAnimateBody(MarkupObject source, MotionSpecSyntax? defaultSpec)
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
            MarkupObject source = CurrentSource;
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

                MarkupObject animationSource = CurrentSource;
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
            MarkupObject source = CurrentSource;
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

            MarkupObject animationSource = CurrentSource;
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
            MarkupObject source = CurrentSource;
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

                MarkupObject assignmentSource = CurrentSource;
                int assignmentOffset = characterIndex;
                string target = ReadMotionTarget();
                SkipWhitespace();
                if (target.Length == 0 || AtEnd || CurrentElement is not null || Read() != '=')
                {
                    throw new DirectiveParseException("Motion property assignment requires '='.", assignmentSource);
                }

                SkipWhitespace();
                MarkupObject valueSource = CurrentSource;
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

    }
}

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
                parts[1] == "timbre" &&
                IsIdentifier(parts[2]) &&
                IsIdentifier(parts[3]))
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
                "$Name.Property, $target.prism.Node.Property, " +
                "$control.parts.$part.Property, or $self.timbre.Handle.Parameter.");
        }

        private string ReadMotionStatement()
        {
            MarkupObject source = CurrentSource;
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

                bool validCount = arguments[1].Text == "forever" ||
                    int.TryParse(arguments[1].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) && count > 0;
                if (!validCount)
                {
                    throw new DirectiveParseException(kind + " requires a positive count or forever.", arguments[1].Location);
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

    }
}

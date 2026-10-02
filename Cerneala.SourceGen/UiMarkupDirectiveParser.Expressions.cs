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
        private static DirectiveExpression ParseExpression(DirectiveHeader header)
        {
            return new DirectiveExpressionParser(header).Parse();
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

    }
}

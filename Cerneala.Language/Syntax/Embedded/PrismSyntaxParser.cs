using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

internal static class PrismSyntaxParser
{
    public static EmbeddedParseResult<DirectiveDocumentSyntax> Parse(string text, int absoluteOffset = 0) =>
        DirectiveSyntaxParser.Parse(text, absoluteOffset, EmbeddedLanguageKind.Prism);

    public static EmbeddedParseResult<PrismCompositionModelSyntax> ParseComposition(
        string text,
        int absoluteOffset = 0) => new ModelParser(text, absoluteOffset).ParseComposition();

    public static EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>> ParseApplications(
        string text,
        int absoluteOffset = 0) => new ModelParser(text, absoluteOffset).ParseApplications();

    private sealed class ModelParser
    {
        private readonly string text;
        private readonly int absoluteOffset;
        private readonly List<EmbeddedDiagnostic> diagnostics = new();
        private int position;

        public ModelParser(string text, int absoluteOffset)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.absoluteOffset = absoluteOffset;
        }

        public EmbeddedParseResult<PrismCompositionModelSyntax> ParseComposition()
        {
            int start = position;
            IReadOnlyList<PrismMemberModelSyntax> members = ParseMembers(stopAtClosingBrace: false);
            return new EmbeddedParseResult<PrismCompositionModelSyntax>(
                new PrismCompositionModelSyntax(members, Span(start, position)),
                diagnostics);
        }

        public EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>> ParseApplications()
        {
            List<PrismApplicationModelSyntax> applications = new();
            while (position < text.Length)
            {
                SkipTrivia();
                if (position >= text.Length)
                {
                    break;
                }

                if (Matches("@prism") && IsKeywordBoundary(position + "@prism".Length))
                {
                    applications.Add(ParseApplication());
                }
                else
                {
                    SkipQuotedOrOne();
                }
            }

            return new EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>>(applications, diagnostics);
        }

        private PrismApplicationModelSyntax ParseApplication()
        {
            int start = position;
            position += "@prism".Length;
            SkipWhitespace();
            if (TryConsume('{'))
            {
                IReadOnlyList<PrismMemberModelSyntax> members = ParseMembers(stopAtClosingBrace: true);
                ValidateApplicationRootMembers(members);
                int end = ConsumeClosingBrace(start, "@prism");
                TryConsume(';');
                PrismCompositionModelSyntax composition = new(members, Span(start, end));
                return new PrismApplicationModelSyntax(null, default, Array.Empty<PrismAssignmentModelSyntax>(), composition, Span(start, position));
            }

            if (!TryConsume('$'))
            {
                Add("PRISM1003", "@prism requires an inline block or a $PrismComposition resource.", position, 1);
                SkipStatement();
                return new PrismApplicationModelSyntax(null, default, Array.Empty<PrismAssignmentModelSyntax>(), null, Span(start, position));
            }

            int referenceStart = position - 1;
            (string name, TextSpan nameSpan) = ReadIdentifier("PrismComposition resource name");
            TextSpan resourceSpan = Span(referenceStart, position);
            List<PrismAssignmentModelSyntax> arguments = new();
            SkipWhitespace();
            if (TryConsume('('))
            {
                while (position < text.Length)
                {
                    SkipTrivia(includeSemicolon: false);
                    if (TryConsume(')'))
                    {
                        break;
                    }

                    PrismAssignmentModelSyntax? argument = ParseAssignment(',', ')');
                    if (argument is not null)
                    {
                        arguments.Add(argument);
                    }

                    SkipWhitespace();
                    if (TryConsume(','))
                    {
                        continue;
                    }

                    if (TryConsume(')'))
                    {
                        break;
                    }

                    Add("PRISM1002", "Prism argument list is missing its closing ')'.", position, 1, transient: true);
                    break;
                }
            }

            SkipWhitespace();
            TryConsume(';');
            return new PrismApplicationModelSyntax(name, resourceSpan, arguments, null, Span(start, position));
        }

        private void ValidateApplicationRootMembers(IReadOnlyList<PrismMemberModelSyntax> members)
        {
            foreach (PrismOperationModelSyntax operation in members.OfType<PrismOperationModelSyntax>())
            {
                string keyword = "@" + operation.Kind.ToString().ToLowerInvariant();
                Add(
                    "PRISM1003",
                    keyword + " is not allowed directly inside @prism.",
                    operation.Span.Start - absoluteOffset,
                    keyword.Length);
            }
        }

        private IReadOnlyList<PrismMemberModelSyntax> ParseMembers(bool stopAtClosingBrace)
        {
            List<PrismMemberModelSyntax> members = new();
            while (position < text.Length)
            {
                SkipTrivia();
                if (position >= text.Length || stopAtClosingBrace && text[position] == '}')
                {
                    break;
                }

                if (text[position] == '@')
                {
                    PrismMemberModelSyntax? directive = ParseDirective();
                    if (directive is not null)
                    {
                        members.Add(directive);
                    }

                    continue;
                }

                PrismAssignmentModelSyntax? assignment = ParseAssignment(';', '}');
                if (assignment is not null)
                {
                    members.Add(assignment);
                }
            }

            return members;
        }

        private PrismMemberModelSyntax? ParseDirective()
        {
            int start = position;
            position++;
            (string keywordName, TextSpan keywordSpan) = ReadIdentifier("Prism directive");
            string keyword = "@" + keywordName;
            switch (keyword)
            {
                case "@parameter":
                    return ParseParameter(start);
                case "@layer":
                    return ParseContainer(start, PrismContainerModelKind.Layer, requireName: true);
                case "@group":
                    return ParseContainer(start, PrismContainerModelKind.Group, requireName: true);
                case "@backdrop":
                    return ParseContainer(start, PrismContainerModelKind.Backdrop, requireName: false);
                case "@filter":
                    return ParseOperation(start, PrismOperationModelKind.Filter, requireType: true);
                case "@style":
                    return ParseOperation(start, PrismOperationModelKind.Style, requireType: true);
                case "@mask":
                    return ParseOperation(start, PrismOperationModelKind.Mask, requireType: false);
                default:
                    Add("PRISM1001", "Unknown Prism directive '" + keyword + "'. Exactly eight Prism directives are supported.", keywordSpan.Start - absoluteOffset - 1, keyword.Length);
                    SkipStatementOrBlock();
                    return null;
            }
        }

        private PrismParameterModelSyntax ParseParameter(int start)
        {
            SkipWhitespace();
            (string name, TextSpan nameSpan) = ReadIdentifier("Prism parameter name");
            SkipWhitespace();
            if (!TryConsume(':'))
            {
                Add("PRISM1003", "Prism parameter '" + name + "' requires a type after ':'.", position, 1);
            }

            SkipWhitespace();
            (string typeName, TextSpan typeSpan) = ReadIdentifier("Prism parameter type");
            SkipWhitespace();
            PrismValueModelSyntax? defaultValue = null;
            if (TryConsume('='))
            {
                defaultValue = ReadValue(';', '}');
            }

            SkipWhitespace();
            TryConsume(';');
            return new PrismParameterModelSyntax(name, nameSpan, typeName, typeSpan, defaultValue, Span(start, position));
        }

        private PrismContainerModelSyntax ParseContainer(int start, PrismContainerModelKind kind, bool requireName)
        {
            SkipWhitespace();
            string? name = null;
            TextSpan nameSpan = default;
            if (position < text.Length && text[position] != '{')
            {
                (name, nameSpan) = ReadIdentifier("Prism node name");
            }
            else if (requireName)
            {
                Add("PRISM1003", "Prism " + kind.ToString().ToLowerInvariant() + " requires a name.", position, 1);
            }

            SkipWhitespace();
            if (!TryConsume('{'))
            {
                Add("PRISM1002", "Prism node is missing its opening '{'.", position, 1, transient: true);
                return new PrismContainerModelSyntax(kind, name, nameSpan, Array.Empty<PrismMemberModelSyntax>(), Span(start, position));
            }

            IReadOnlyList<PrismMemberModelSyntax> members = ParseMembers(stopAtClosingBrace: true);
            int end = ConsumeClosingBrace(start);
            return new PrismContainerModelSyntax(kind, name, nameSpan, members, Span(start, end));
        }

        private PrismOperationModelSyntax ParseOperation(int start, PrismOperationModelKind kind, bool requireType)
        {
            SkipWhitespace();
            string? typeName = null;
            TextSpan typeSpan = default;
            if (position < text.Length && text[position] != '{')
            {
                (typeName, typeSpan) = ReadIdentifier("Prism operation symbol");
            }
            else if (requireType)
            {
                Add("PRISM1003", "Prism " + kind.ToString().ToLowerInvariant() + " requires a catalog symbol.", position, 1);
            }

            SkipWhitespace();
            if (!TryConsume('{'))
            {
                Add("PRISM1002", "Prism operation is missing its opening '{'.", position, 1, transient: true);
                return new PrismOperationModelSyntax(kind, typeName, typeSpan, Array.Empty<PrismMemberModelSyntax>(), Span(start, position));
            }

            IReadOnlyList<PrismMemberModelSyntax> members = ParseMembers(stopAtClosingBrace: true);
            int end = ConsumeClosingBrace(start);
            return new PrismOperationModelSyntax(kind, typeName, typeSpan, members, Span(start, end));
        }

        private PrismAssignmentModelSyntax? ParseAssignment(char terminator, char alternateTerminator)
        {
            int start = position;
            SkipWhitespace();
            (string name, TextSpan nameSpan) = ReadIdentifier("Prism property or parameter name");
            if (name.Length == 0)
            {
                SkipStatement();
                return null;
            }

            SkipWhitespace();
            if (!TryConsume('='))
            {
                Add("PRISM1003", "Prism assignment '" + name + "' requires '='.", position, 1);
                SkipStatement();
                return null;
            }

            PrismValueModelSyntax value = ReadValue(terminator, alternateTerminator);
            SkipWhitespace();
            return new PrismAssignmentModelSyntax(name, nameSpan, value, Span(start, position));
        }

        private PrismValueModelSyntax ReadValue(char terminator, char alternateTerminator)
        {
            SkipWhitespace();
            int start = position;
            int parentheses = 0;
            bool quoted = false;
            char quote = '\0';
            while (position < text.Length)
            {
                char character = text[position];
                if (quoted)
                {
                    position++;
                    if (character == quote && (position < 2 || text[position - 2] != '\\'))
                    {
                        quoted = false;
                    }

                    continue;
                }

                if (character is '\'' or '"')
                {
                    quoted = true;
                    quote = character;
                    position++;
                    continue;
                }

                if (character == '(')
                {
                    parentheses++;
                }
                else if (character == ')' && parentheses > 0)
                {
                    parentheses--;
                }
                else if (parentheses == 0 && (character == terminator || character == alternateTerminator))
                {
                    break;
                }

                position++;
            }

            int end = position;
            while (end > start && char.IsWhiteSpace(text[end - 1]))
            {
                end--;
            }

            string value = text.Substring(start, end - start);
            return new PrismValueModelSyntax(value, ClassifyValue(value), Span(start, end));
        }

        private int ConsumeClosingBrace(int ownerStart, string ownerName = "Prism block")
        {
            SkipWhitespace();
            if (TryConsume('}'))
            {
                return position;
            }

            Add("PRISM1002", ownerName + " is missing its closing '}'.", ownerStart, Math.Min(6, text.Length - ownerStart), transient: true);
            return position;
        }

        private (string Text, TextSpan Span) ReadIdentifier(string description)
        {
            int start = position;
            while (position < text.Length &&
                (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '.'))
            {
                position++;
            }

            if (position == start)
            {
                Add("PRISM1003", description + " is missing.", position, 1);
                return (string.Empty, Span(start, start));
            }

            return (text.Substring(start, position - start), Span(start, position));
        }

        private void SkipTrivia(bool includeSemicolon = true)
        {
            while (position < text.Length &&
                (char.IsWhiteSpace(text[position]) || includeSemicolon && text[position] == ';'))
            {
                position++;
            }
        }

        private void SkipWhitespace()
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
        }

        private void SkipQuotedOrOne()
        {
            if (position >= text.Length || text[position] is not ('\'' or '"'))
            {
                position++;
                return;
            }

            char quote = text[position++];
            while (position < text.Length)
            {
                char character = text[position++];
                if (character == quote && (position < 2 || text[position - 2] != '\\'))
                {
                    return;
                }
            }
        }

        private void SkipStatementOrBlock()
        {
            SkipWhitespace();
            if (!TryConsume('{'))
            {
                SkipStatement();
                return;
            }

            int depth = 1;
            while (position < text.Length && depth > 0)
            {
                if (text[position] == '{')
                {
                    depth++;
                }
                else if (text[position] == '}')
                {
                    depth--;
                }

                SkipQuotedOrOne();
            }
        }

        private void SkipStatement()
        {
            while (position < text.Length && text[position] is not (';' or '}' or '\r' or '\n'))
            {
                SkipQuotedOrOne();
            }

            if (position < text.Length && text[position] == ';')
            {
                position++;
            }
        }

        private bool TryConsume(char character)
        {
            if (position >= text.Length || text[position] != character)
            {
                return false;
            }

            position++;
            return true;
        }

        private bool Matches(string value) =>
            position + value.Length <= text.Length &&
            string.CompareOrdinal(text, position, value, 0, value.Length) == 0;

        private bool IsKeywordBoundary(int index) =>
            index >= text.Length || !char.IsLetterOrDigit(text[index]) && text[index] != '_';

        private TextSpan Span(int relativeStart, int relativeEnd) =>
            new(absoluteOffset + relativeStart, Math.Max(0, relativeEnd - relativeStart));

        private void Add(string id, string message, int relativeStart, int length, bool transient = false) =>
            diagnostics.Add(new EmbeddedDiagnostic(
                id,
                message,
                new TextSpan(absoluteOffset + Math.Min(relativeStart, text.Length), Math.Max(0, Math.Min(length, text.Length - Math.Min(relativeStart, text.Length)))),
                transient));

        private static PrismValueModelKind ClassifyValue(string value)
        {
            if (value == "null")
            {
                return PrismValueModelKind.NullLiteral;
            }

            if (value.StartsWith("$", StringComparison.Ordinal))
            {
                if (value.IndexOf('.') >= 0)
                {
                    return value.IndexOf(':') >= 0
                        ? PrismValueModelKind.Binding
                        : PrismValueModelKind.DirectReference;
                }

                return PrismValueModelKind.ResourceReference;
            }

            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                return PrismValueModelKind.ColorLiteral;
            }

            if (value.Length >= 2 && value[0] is '\'' or '"' && value[value.Length - 1] == value[0])
            {
                return PrismValueModelKind.StringLiteral;
            }

            if (value.StartsWith("(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal))
            {
                return PrismValueModelKind.TupleLiteral;
            }

            if (bool.TryParse(value, out _))
            {
                return PrismValueModelKind.BooleanLiteral;
            }

            if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return PrismValueModelKind.NumberLiteral;
            }

            return PrismValueModelKind.Identifier;
        }
    }
}

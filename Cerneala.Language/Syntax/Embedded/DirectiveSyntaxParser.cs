using Cerneala.Language.Text;

namespace Cerneala.Language.Syntax.Embedded;

internal static class DirectiveSyntaxParser
{
    private static readonly string[] generalKeywords =
    [
        "@template", "@when", "@if", "@default"
    ];

    private static readonly string[] motionKeywords =
    [
        "@when", "@if", "@on", "@presence", "@layout", "@scroll", "@drag", "@gesture",
        "@set", "@animate", "@keyframes", "@stagger", "@parallel", "@sequence", "@run",
        "@cancel", "@handle", "@parameter", "@from", "@to", "@default", "@template"
    ];

    private static readonly string[] prismKeywords =
    [
        "@prism", "@parameter", "@layer", "@group", "@filter", "@style", "@mask", "@backdrop"
    ];

    public static EmbeddedParseResult<DirectiveDocumentSyntax> Parse(
        string text,
        int absoluteOffset = 0,
        EmbeddedLanguageKind language = EmbeddedLanguageKind.Directive)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        IReadOnlyList<string> keywords = language switch
        {
            EmbeddedLanguageKind.Motion => motionKeywords,
            EmbeddedLanguageKind.Prism => prismKeywords,
            _ => generalKeywords.Concat(motionKeywords).Concat(prismKeywords)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };
        List<DirectiveSyntax> directives = new();
        List<AssignmentSyntax> assignments = new();
        List<DirectiveBlockSyntax> blocks = new();
        List<EmbeddedDiagnostic> diagnostics = new();
        Stack<int> braces = new();
        Stack<int> parentheses = new();
        bool quoted = false;
        char quote = '\0';
        bool escaped = false;

        for (int position = 0; position < text.Length; position++)
        {
            char character = text[position];
            if (quoted)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
                continue;
            }

            if (character == '@')
            {
                int start = position++;
                while (position < text.Length && IsIdentifierPart(text[position]))
                {
                    position++;
                }

                string keyword = text.Substring(start, position - start);
                position--;
                TextSpan span = new(absoluteOffset + start, keyword.Length);
                if (!keywords.Contains(keyword, StringComparer.Ordinal))
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        language == EmbeddedLanguageKind.Prism ? "PRISM1001" : DiagnosticId(language),
                        "Unknown " + LanguageName(language) + " directive '" + keyword + "'.",
                        span));
                }
                else
                {
                    directives.Add(new DirectiveSyntax(keyword, span, braces.Count));
                }

                continue;
            }

            switch (character)
            {
                case '{':
                    braces.Push(position);
                    break;
                case '}':
                    if (braces.Count == 0)
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(
                            MissingDelimiterId(language),
                            "Unexpected closing '}'.",
                            new TextSpan(absoluteOffset + position, 1)));
                    }
                    else
                    {
                        int opening = braces.Pop();
                        blocks.Add(new DirectiveBlockSyntax(
                            new TextSpan(absoluteOffset + opening, position - opening + 1)));
                    }
                    break;
                case '(':
                    parentheses.Push(position);
                    break;
                case ')':
                    if (parentheses.Count == 0)
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(
                            MissingDelimiterId(language),
                            "Unexpected closing ')'.",
                            new TextSpan(absoluteOffset + position, 1)));
                    }
                    else
                    {
                        parentheses.Pop();
                    }
                    break;
                case '=' when !IsComparator(text, position):
                    AssignmentSyntax? assignment = TryReadAssignment(text, position, absoluteOffset);
                    if (assignment is not null)
                    {
                        assignments.Add(assignment);
                    }
                    break;
            }
        }

        if (quoted)
        {
            int quoteStart = text.LastIndexOf(quote);
            diagnostics.Add(new EmbeddedDiagnostic(
                MissingDelimiterId(language),
                "String literal is missing its closing quote.",
                new TextSpan(absoluteOffset + Math.Max(0, quoteStart), 1),
                transient: true));
        }
        else if (parentheses.Count > 0)
        {
            int start = parentheses.Peek();
            diagnostics.Add(new EmbeddedDiagnostic(
                MissingDelimiterId(language),
                "Expression is missing its closing ')'.",
                new TextSpan(absoluteOffset + start, 1),
                transient: true));
        }
        else if (braces.Count > 0)
        {
            int start = braces.Peek();
            diagnostics.Add(new EmbeddedDiagnostic(
                MissingDelimiterId(language),
                "Directive block is missing its closing '}'.",
                new TextSpan(absoluteOffset + start, 1),
                transient: true));
        }

        foreach (int opening in braces)
        {
            blocks.Add(new DirectiveBlockSyntax(
                new TextSpan(absoluteOffset + opening, text.Length - opening)));
        }

        return new EmbeddedParseResult<DirectiveDocumentSyntax>(
            new DirectiveDocumentSyntax(
                text,
                absoluteOffset,
                language,
                directives,
                assignments,
                blocks.OrderBy(block => block.Span.Start).ToArray()),
            diagnostics);
    }

    private static AssignmentSyntax? TryReadAssignment(string text, int equals, int absoluteOffset)
    {
        int nameEnd = equals;
        while (nameEnd > 0 && char.IsWhiteSpace(text[nameEnd - 1]))
        {
            nameEnd--;
        }

        int nameStart = nameEnd;
        while (nameStart > 0 && IsPathPart(text[nameStart - 1]))
        {
            nameStart--;
        }

        if (nameStart == nameEnd)
        {
            return null;
        }

        int valueStart = equals + 1;
        while (valueStart < text.Length && char.IsWhiteSpace(text[valueStart]))
        {
            valueStart++;
        }

        int valueEnd = valueStart;
        bool quoted = false;
        char quote = '\0';
        int parentheses = 0;
        while (valueEnd < text.Length)
        {
            char character = text[valueEnd];
            if (quoted)
            {
                if (character == quote && (valueEnd == valueStart || text[valueEnd - 1] != '\\'))
                {
                    quoted = false;
                }
            }
            else if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')')
            {
                parentheses--;
            }
            else if (parentheses == 0 && (character is ';' or '}' or '\r' or '\n') &&
                !(character == ';' && IsXmlEntityTerminator(text, valueEnd)))
            {
                break;
            }

            valueEnd++;
        }

        while (valueEnd > valueStart && char.IsWhiteSpace(text[valueEnd - 1]))
        {
            valueEnd--;
        }

        return new AssignmentSyntax(
            text.Substring(nameStart, nameEnd - nameStart),
            new TextSpan(absoluteOffset + nameStart, nameEnd - nameStart),
            new TextSpan(absoluteOffset + valueStart, valueEnd - valueStart));
    }

    private static bool IsComparator(string text, int position)
    {
        return position > 0 && text[position - 1] is '=' or '!' or '<' or '>' ||
            position + 1 < text.Length && text[position + 1] == '=';
    }

    private static bool IsIdentifierPart(char character) => char.IsLetterOrDigit(character) || character == '_';

    private static bool IsPathPart(char character) => IsIdentifierPart(character) || character is '.' or '$';

    private static bool IsXmlEntityTerminator(string text, int index)
    {
        int ampersand = index - 1;
        while (ampersand >= 0 && (char.IsLetterOrDigit(text[ampersand]) || text[ampersand] == '#'))
        {
            ampersand--;
        }

        return ampersand >= 0 && text[ampersand] == '&' && ampersand + 1 < index;
    }

    private static string DiagnosticId(EmbeddedLanguageKind language) =>
        language == EmbeddedLanguageKind.Motion ? "CERNEALAUI020" : "CERNEALAUI006";

    private static string MissingDelimiterId(EmbeddedLanguageKind language) =>
        language == EmbeddedLanguageKind.Prism ? "PRISM1002" : DiagnosticId(language);

    private static string LanguageName(EmbeddedLanguageKind language) =>
        language == EmbeddedLanguageKind.Directive ? "markup" : language.ToString();
}

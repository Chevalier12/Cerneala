using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed partial class CernealaCompletionService
{
    private static string? FindPrismOperationSymbolCompletionKind(string statement)
    {
        string trimmed = statement.TrimStart();
        foreach (string kind in new[] { "filter", "style" })
        {
            string keyword = "@" + kind;
            if (trimmed.StartsWith(keyword, StringComparison.Ordinal) &&
                trimmed.Length > keyword.Length &&
                char.IsWhiteSpace(trimmed[keyword.Length]) &&
                trimmed.IndexOfAny(['{', '}', '=', ';', '(', ')']) < 0)
            {
                return kind;
            }
        }

        return null;
    }

    private static PrismCompletionContext? FindPrismCompletionContext(string source, int offset)
    {
        Stack<PrismCompletionContext?> blocks = new();
        bool quoted = false;
        char quote = '\0';
        for (int index = 0; index < offset; index++)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '{')
            {
                blocks.Push(CreatePrismCompletionContext(source, index));
            }
            else if (character == '}' && blocks.Count > 0)
            {
                blocks.Pop();
            }
        }

        return blocks.Count == 0 ? null : blocks.Peek();
    }

    private static PrismCompletionContext? CreatePrismCompletionContext(string source, int openingBrace)
    {
        string? keyword = FindDirectiveKeywordBeforeBody(source, openingBrace);
        string? kind = keyword switch
        {
            "@prism" => "composition",
            "@layer" => "layer",
            "@group" => "group",
            "@filter" => "filter",
            "@style" => "style",
            "@mask" => "mask",
            _ => null
        };
        if (kind is null)
        {
            return null;
        }

        string? symbol = null;
        if (kind is "filter" or "style")
        {
            int at = source.LastIndexOf('@', Math.Max(0, openingBrace - 1));
            int symbolStart = at + keyword!.Length;
            while (symbolStart < openingBrace && char.IsWhiteSpace(source[symbolStart]))
            {
                symbolStart++;
            }

            int symbolEnd = symbolStart;
            while (symbolEnd < openingBrace && IsIdentifierCharacter(source[symbolEnd]))
            {
                symbolEnd++;
            }

            if (symbolEnd > symbolStart)
            {
                symbol = source.Substring(symbolStart, symbolEnd - symbolStart);
            }
        }

        return new PrismCompletionContext(kind, symbol);
    }

    private static bool IsInsideBraceBody(string source, int offset)
    {
        int closingDepth = 0;
        for (int index = offset - 1; index >= 0; index--)
        {
            if (source[index] == '}')
            {
                closingDepth++;
            }
            else if (source[index] == '{')
            {
                if (closingDepth == 0)
                {
                    return true;
                }

                closingDepth--;
            }
        }

        return false;
    }

    private static ReferenceSite? FindReferenceSite(string source, int offset)
    {
        int identifierStart = offset;
        while (identifierStart > 0 && IsIdentifierCharacter(source[identifierStart - 1]))
        {
            identifierStart--;
        }

        if (identifierStart <= 0 || source[identifierStart - 1] != '$')
        {
            return null;
        }

        int referenceStart = identifierStart - 1;
        return new ReferenceSite(new TextSpan(referenceStart, offset - referenceStart));
    }

    private static ReferenceMemberSite? FindReferenceMemberSite(string source, int offset)
    {
        int memberStart = offset;
        while (memberStart > 0 && IsIdentifierCharacter(source[memberStart - 1]))
        {
            memberStart--;
        }

        if (memberStart <= 0 || source[memberStart - 1] != '.')
        {
            return null;
        }

        int ownerEnd = memberStart - 1;
        int ownerStart = ownerEnd;
        while (ownerStart > 0 &&
            (IsIdentifierCharacter(source[ownerStart - 1]) || source[ownerStart - 1] == '.'))
        {
            ownerStart--;
        }

        if (ownerStart <= 0 || source[ownerStart - 1] != '$')
        {
            return null;
        }

        string[] ownerSegments = source.Substring(ownerStart, ownerEnd - ownerStart).Split('.');
        if (ownerSegments.Length == 0 || ownerSegments.Any(segment => segment.Length == 0))
        {
            return null;
        }

        return new ReferenceMemberSite(
            ownerSegments,
            new TextSpan(memberStart, offset - memberStart));
    }

    private static HashSet<string> ReadAttributeNames(string source, int tagStart, int offset)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        int position = tagStart + 1;
        while (position < offset && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        while (position < offset)
        {
            while (position < offset && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            int start = position;
            while (position < offset && IsMarkupNameCharacter(source[position]))
            {
                position++;
            }

            if (position > start)
            {
                names.Add(source.Substring(start, position - start));
            }

            while (position < offset && source[position] is not ('\'' or '"'))
            {
                position++;
            }

            if (position < offset)
            {
                char quote = source[position++];
                while (position < offset && source[position] != quote)
                {
                    position++;
                }

                position = Math.Min(offset, position + 1);
            }
        }

        return names;
    }

    private static string? FindUnclosedElementName(string source, int offset)
    {
        List<string> stack = new();
        int position = 0;
        while (position < offset)
        {
            int opening = source.IndexOf('<', position);
            if (opening < 0 || opening >= offset)
            {
                break;
            }

            int end = source.IndexOf('>', opening + 1);
            if (end < 0 || end >= offset)
            {
                break;
            }

            string tag = source.Substring(opening + 1, end - opening - 1).Trim();
            if (tag.StartsWith("/", StringComparison.Ordinal))
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }
            }
            else if (!tag.StartsWith("!", StringComparison.Ordinal) && !tag.StartsWith("?", StringComparison.Ordinal) &&
                !tag.EndsWith("/", StringComparison.Ordinal))
            {
                stack.Add(tag.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
            }

            position = end + 1;
        }

        return stack.LastOrDefault();
    }

    private static FunctionCall? FindFunctionCall(string source, int offset)
    {
        int depth = 0;
        int commas = 0;
        bool quoted = false;
        char quote = '\0';
        for (int index = offset - 1; index >= 0; index--)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == ')')
            {
                depth++;
            }
            else if (character == '(')
            {
                if (depth > 0)
                {
                    depth--;
                    continue;
                }

                int end = index;
                int start = end;
                while (start > 0 && IsIdentifierCharacter(source[start - 1]))
                {
                    start--;
                }

                return end == start ? null : new FunctionCall(source.Substring(start, end - start), commas);
            }
            else if (character == ',' && depth == 0)
            {
                commas++;
            }
            else if (character is '{' or '}' or ';' && depth == 0)
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsInsideAnyDirective(string source, int offset, IEnumerable<string> keywords) =>
        keywords.Any(keyword => IsInsideDirective(source, offset, keyword));

    private static string? FindInnermostDirectiveKeyword(string source, int offset)
    {
        Stack<string?> blocks = new();
        bool quoted = false;
        char quote = '\0';
        for (int index = 0; index < offset; index++)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '{')
            {
                blocks.Push(FindDirectiveKeywordBeforeBody(source, index));
            }
            else if (character == '}' && blocks.Count > 0)
            {
                blocks.Pop();
            }
        }

        return blocks.Count == 0 ? null : blocks.Peek();
    }

    private static string? FindDirectiveKeywordBeforeBody(string source, int openingBrace)
    {
        int at = source.LastIndexOf('@', Math.Max(0, openingBrace - 1));
        if (at < 0)
        {
            return null;
        }

        for (int index = at + 1; index < openingBrace; index++)
        {
            if (source[index] is '{' or '}' or ';' or '<' or '>')
            {
                return null;
            }
        }

        int end = at + 1;
        while (end < openingBrace && IsIdentifierCharacter(source[end]))
        {
            end++;
        }

        return end == at + 1 ? null : source.Substring(at, end - at);
    }

    private static string GetEmbeddedStatementPrefix(string source, int offset)
    {
        int start = offset;
        while (start > 0 && source[start - 1] is not ('\r' or '\n' or '{' or '}' or ';'))
        {
            start--;
        }

        return source.Substring(start, offset - start);
    }

    private static bool IsMotionHandleCompletionSite(string statement)
    {
        string trimmed = statement.TrimStart();
        if (trimmed.StartsWith("@cancel", StringComparison.Ordinal))
        {
            string suffix = trimmed.Substring("@cancel".Length).TrimStart();
            return suffix.All(IsIdentifierCharacter);
        }

        if (!trimmed.StartsWith("@run", StringComparison.Ordinal))
        {
            return false;
        }

        int separator = trimmed.LastIndexOf(" as ", StringComparison.Ordinal);
        return separator >= 0 && trimmed.Substring(separator + 4).All(IsIdentifierCharacter);
    }

    private static bool TryGetReactiveExpressionOperandContext(
        string statement,
        out bool includeWhenValue)
    {
        int whenStart = FindLastDirectiveKeyword(statement, "@when");
        int ifStart = FindLastDirectiveKeyword(statement, "@if");
        int keywordStart = Math.Max(whenStart, ifStart);
        string? keyword = keywordStart == whenStart && whenStart >= 0
            ? "@when"
            : ifStart >= 0 ? "@if" : null;
        includeWhenValue = keyword == "@if";
        return keyword is not null &&
            IsReactiveExpressionOperandSite(
                statement.Substring(keywordStart + keyword.Length));
    }

    private static bool IsOnEventNameCompletionSite(string statement)
    {
        int onStart = FindLastDirectiveKeyword(statement, "@on");
        if (onStart < 0)
        {
            return false;
        }

        string candidate = statement.Substring(onStart + "@on".Length).TrimStart();
        return candidate.All(IsIdentifierCharacter);
    }

    private static bool IsReactiveExpressionOperandSite(string expression)
    {
        string rightTrimmed = expression.TrimEnd();
        if (rightTrimmed.Length == 0)
        {
            return true;
        }

        if (rightTrimmed.EndsWith("(", StringComparison.Ordinal) ||
            EndsWithComparisonOperator(rightTrimmed))
        {
            return true;
        }

        int wordStart = rightTrimmed.Length;
        while (wordStart > 0 && IsIdentifierCharacter(rightTrimmed[wordStart - 1]))
        {
            wordStart--;
        }

        string lastWord = rightTrimmed.Substring(wordStart);
        bool hasTrailingWhitespace = rightTrimmed.Length < expression.Length;
        if (hasTrailingWhitespace)
        {
            return lastWord is "and" or "or";
        }

        string beforeWord = rightTrimmed.Substring(0, wordStart).TrimEnd();
        return beforeWord.Length == 0 ||
            beforeWord.EndsWith("(", StringComparison.Ordinal) ||
            EndsWithComparisonOperator(beforeWord) ||
            EndsWithLogicalOperator(beforeWord);
    }

    private static bool EndsWithComparisonOperator(string text) =>
        text.EndsWith("==", StringComparison.Ordinal) ||
        text.EndsWith("!=", StringComparison.Ordinal) ||
        text.EndsWith("<=", StringComparison.Ordinal) ||
        text.EndsWith(">=", StringComparison.Ordinal) ||
        text.EndsWith("<", StringComparison.Ordinal) ||
        text.EndsWith(">", StringComparison.Ordinal);

    private static bool EndsWithLogicalOperator(string text)
    {
        int end = text.Length;
        int start = end;
        while (start > 0 && IsIdentifierCharacter(text[start - 1]))
        {
            start--;
        }

        string word = text.Substring(start, end - start);
        return word is "and" or "or";
    }

    private static int FindLastDirectiveKeyword(string text, string keyword)
    {
        int start = text.LastIndexOf(keyword, StringComparison.Ordinal);
        if (start < 0)
        {
            return -1;
        }

        int end = start + keyword.Length;
        bool validStart = start == 0 || char.IsWhiteSpace(text[start - 1]) ||
            text[start - 1] is '>' or '{' or '}';
        bool validEnd = end == text.Length || char.IsWhiteSpace(text[end]);
        return validStart && validEnd ? start : -1;
    }

    private static bool IsInsideDirective(string source, int offset, string keyword)
    {
        int start = source.LastIndexOf(keyword, Math.Max(0, offset - 1), StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        int opening = source.IndexOf('{', start + keyword.Length);
        if (opening < 0 || opening >= offset)
        {
            return false;
        }

        int depth = 1;
        for (int index = opening + 1; index < offset; index++)
        {
            depth += source[index] == '{' ? 1 : source[index] == '}' ? -1 : 0;
            if (depth == 0)
            {
                // The directive block closed before the offset.
                return false;
            }
        }

        return depth > 0;
    }

    private static bool IsMarkupNameCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '_' or ':' or '.' or '-';

    private static bool IsIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';

    private sealed record FunctionCall(string Name, int ActiveParameter);

    private sealed record ReferenceSite(TextSpan ReplacementSpan);

    private sealed record ReferenceMemberSite(
        IReadOnlyList<string> OwnerSegments,
        TextSpan ReplacementSpan);

    private enum CompletionSiteKind
    {
        Element,
        Attribute,
        AttributeValue,
        Directive
    }

    private sealed class CompletionSite
    {
        private CompletionSite(
            string source,
            int offset,
            CompletionSiteKind kind,
            int tagStart,
            bool tagHasClose,
            bool isClosingTag,
            bool isRootTag,
            TextSpan wordSpan,
            string wordPrefix,
            int lineStart,
            string? attributeName,
            string valuePrefix,
            TextSpan valueWordSpan,
            BindingSite? binding)
        {
            Source = source;
            Offset = offset;
            Kind = kind;
            TagStart = tagStart;
            TagHasClose = tagHasClose;
            IsClosingTag = isClosingTag;
            IsRootTag = isRootTag;
            WordSpan = wordSpan;
            WordPrefix = wordPrefix;
            LineStart = lineStart;
            AttributeName = attributeName;
            ValuePrefix = valuePrefix;
            ValueWordSpan = valueWordSpan;
            Binding = binding;
        }

        public string Source { get; }
        public int Offset { get; }
        public CompletionSiteKind Kind { get; }
        public int TagStart { get; }
        public bool TagHasClose { get; }
        public bool IsClosingTag { get; }
        public bool IsRootTag { get; }
        public TextSpan WordSpan { get; }
        public string WordPrefix { get; }
        public int LineStart { get; }
        public string? AttributeName { get; }
        public string ValuePrefix { get; }
        public TextSpan ValueWordSpan { get; }
        public BindingSite? Binding { get; }

        public static CompletionSite Classify(string source, int offset)
        {
            int lineStart = source.LastIndexOf('\n', Math.Max(0, offset - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            int wordStart = offset;
            while (wordStart > lineStart && (IsMarkupNameCharacter(source[wordStart - 1]) || source[wordStart - 1] == '@'))
            {
                wordStart--;
            }

            TextSpan wordSpan = new(wordStart, offset - wordStart);
            string wordPrefix = source.Substring(wordStart, offset - wordStart);
            int tagStart = source.LastIndexOf('<', Math.Max(0, offset - 1));
            int previousEnd = source.LastIndexOf('>', Math.Max(0, offset - 1));
            bool insideTag = tagStart >= 0 && tagStart > previousEnd;
            if (!insideTag)
            {
                return new CompletionSite(source, offset, CompletionSiteKind.Directive, -1, false, false, false,
                    wordSpan, wordPrefix, lineStart, null, string.Empty, wordSpan, null);
            }

            int tagEnd = source.IndexOf('>', offset);
            bool tagHasClose = tagEnd >= 0;
            bool closing = tagStart + 1 < source.Length && source[tagStart + 1] == '/';
            int nameStart = tagStart + (closing ? 2 : 1);
            int nameEnd = nameStart;
            while (nameEnd < source.Length && IsMarkupNameCharacter(source[nameEnd]))
            {
                nameEnd++;
            }

            bool elementSite = offset <= nameEnd &&
                !source.Substring(nameStart, Math.Max(0, offset - nameStart)).Any(char.IsWhiteSpace);
            bool rootTag = source.Substring(0, tagStart).All(character => char.IsWhiteSpace(character) || character == '\uFEFF');
            if (elementSite)
            {
                TextSpan elementWord = new(nameStart, Math.Max(0, offset - nameStart));
                return new CompletionSite(source, offset, CompletionSiteKind.Element, tagStart, tagHasClose, closing, rootTag,
                    elementWord, source.Substring(elementWord.Start, elementWord.Length), lineStart, null, string.Empty,
                    elementWord, null);
            }

            bool quoted = false;
            char quote = '\0';
            int valueStart = -1;
            for (int index = nameEnd; index < offset; index++)
            {
                if (!quoted && source[index] is '\'' or '"')
                {
                    quoted = true;
                    quote = source[index];
                    valueStart = index + 1;
                }
                else if (quoted && source[index] == quote)
                {
                    quoted = false;
                    valueStart = -1;
                }
            }

            if (quoted && valueStart >= 0)
            {
                string? attributeName = FindAttributeName(source, valueStart - 1, tagStart);
                int valueWordStart = offset;
                while (valueWordStart > valueStart && !char.IsWhiteSpace(source[valueWordStart - 1]) &&
                    source[valueWordStart - 1] is not ('(' or ',' or '='))
                {
                    valueWordStart--;
                }

                TextSpan valueWordSpan = new(valueWordStart, offset - valueWordStart);
                string valuePrefix = source.Substring(valueStart, offset - valueStart);
                BindingSite? binding = BindingSite.Parse(source, valueStart, offset, valuePrefix);
                return new CompletionSite(source, offset, CompletionSiteKind.AttributeValue, tagStart, tagHasClose, false,
                    rootTag, wordSpan, wordPrefix, lineStart, attributeName, valuePrefix, valueWordSpan, binding);
            }

            int attributeStart = offset;
            while (attributeStart > nameEnd && IsMarkupNameCharacter(source[attributeStart - 1]))
            {
                attributeStart--;
            }

            TextSpan attributeWord = new(attributeStart, offset - attributeStart);
            return new CompletionSite(source, offset, CompletionSiteKind.Attribute, tagStart, tagHasClose, false, rootTag,
                attributeWord, source.Substring(attributeWord.Start, attributeWord.Length), lineStart, null,
                string.Empty, attributeWord, null);
        }

        private static string? FindAttributeName(string source, int quoteIndex, int tagStart)
        {
            int equals = quoteIndex - 1;
            while (equals > tagStart && char.IsWhiteSpace(source[equals]))
            {
                equals--;
            }

            if (equals <= tagStart || source[equals] != '=')
            {
                return null;
            }

            int end = equals;
            int start = end;
            while (start > tagStart && IsMarkupNameCharacter(source[start - 1]))
            {
                start--;
            }

            return source.Substring(start, end - start);
        }
    }

    private sealed class BindingSite
    {
        private BindingSite(
            IReadOnlyList<string> segments,
            TextSpan replacementSpan,
            bool isMode,
            bool isDirect)
        {
            Segments = segments;
            ReplacementSpan = replacementSpan;
            IsMode = isMode;
            IsDirect = isDirect;
        }

        public IReadOnlyList<string> Segments { get; }
        public TextSpan ReplacementSpan { get; }
        public bool IsMode { get; }
        public bool IsDirect { get; }

        public static BindingSite? Parse(string source, int valueStart, int offset, string valuePrefix)
        {
            int expressionStart = valuePrefix.LastIndexOf('$');
            while (expressionStart > 0 && valuePrefix[expressionStart - 1] == '.')
            {
                int previous = valuePrefix.LastIndexOf('$', expressionStart - 2);
                if (previous < 0)
                {
                    break;
                }

                expressionStart = previous;
            }

            if (expressionStart < 0)
            {
                return null;
            }

            string expression = valuePrefix.Substring(expressionStart);
            int firstNonWhitespace = 0;
            while (firstNonWhitespace < valuePrefix.Length && char.IsWhiteSpace(valuePrefix[firstNonWhitespace]))
            {
                firstNonWhitespace++;
            }

            bool isDirect = expressionStart == firstNonWhitespace;
            int mode = expression.LastIndexOf(':');
            if (mode >= 0 && expression.IndexOf(' ', mode) < 0)
            {
                TextSpan replacement = new(valueStart + expressionStart + mode + 1, expression.Length - mode - 1);
                string path = expression.Substring(0, mode);
                return new BindingSite(path.Split('.'), replacement, true, isDirect);
            }

            string[] segments = expression.Split('.');
            int segmentStart = expression.LastIndexOf('.') + 1;
            if (segments.Length == 1)
            {
                segmentStart = 0;
            }

            TextSpan span = new(valueStart + expressionStart + segmentStart, expression.Length - segmentStart);
            if (segments.Length == 1)
            {
                span = new TextSpan(valueStart + expressionStart, expression.Length);
            }

            return new BindingSite(segments, span, false, isDirect);
        }
    }

    private sealed class PrismCompletionContext
    {
        public PrismCompletionContext(string kind, string? symbol)
        {
            Kind = kind;
            Symbol = symbol;
        }

        public string Kind { get; }

        public string? Symbol { get; }
    }

}

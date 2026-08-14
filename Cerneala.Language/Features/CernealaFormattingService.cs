using System.Text;
using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed class CernealaFormattingService
{
    public IReadOnlyList<CernealaFormattingEdit> FormatDocument(
        CernealaDocument document,
        CernealaFormattingOptions options,
        CancellationToken cancellationToken = default) =>
        Format(document, options, document.Syntax.Span, cancellationToken);

    public IReadOnlyList<CernealaFormattingEdit> FormatRange(
        CernealaDocument document,
        TextSpan range,
        CernealaFormattingOptions options,
        CancellationToken cancellationToken = default) =>
        Format(document, options, range, cancellationToken);

    public IReadOnlyList<CernealaFormattingEdit> FormatOnType(
        CernealaDocument document,
        int offset,
        CernealaFormattingOptions options,
        CancellationToken cancellationToken = default)
    {
        LinePosition position = document.Text.GetLinePosition(offset);
        int start = document.Text.GetOffset(new LinePosition(position.Line, 0));
        int end = position.Line + 1 < document.Text.LineCount
            ? document.Text.GetOffset(new LinePosition(position.Line + 1, 0))
            : document.Text.Length;
        return Format(document, options, new TextSpan(start, end - start), cancellationToken);
    }

    private static IReadOnlyList<CernealaFormattingEdit> Format(
        CernealaDocument document,
        CernealaFormattingOptions options,
        TextSpan range,
        CancellationToken cancellationToken)
    {
        string source = document.Text.ToString();
        TextLineInfo[] lines = GetLines(source);
        if (lines.Length == 0)
        {
            return Array.Empty<CernealaFormattingEdit>();
        }

        int firstLine = document.Text.GetLinePosition(Math.Min(range.Start, document.Text.Length)).Line;
        int lastOffset = Math.Min(range.End, document.Text.Length);
        int lastLine = document.Text.GetLinePosition(lastOffset).Line;
        if (range.Length > 0 && lastOffset > 0 && lastOffset == lines[lastLine].Start)
        {
            lastLine--;
        }

        firstLine = Math.Max(0, firstLine);
        lastLine = Math.Max(firstLine, Math.Min(lastLine, lines.Length - 1));
        ElementSyntax[] elements = document.Syntax.DescendantElements().ToArray();
        SyntaxNode[] nodes = DescendantNodes(document.Syntax).ToArray();
        StringBuilder replacement = new();
        bool changed = false;
        for (int lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TextLineInfo line = lines[lineIndex];
            string original = source.Substring(line.Start, line.ContentLength);
            string trimmed = original.TrimStart(' ', '\t');
            string formatted = original;
            if (trimmed.Length == 0)
            {
                formatted = string.Empty;
            }
            else
            {
                int contentOffset = line.Start + original.Length - trimmed.Length;
                TextSyntax? textNode = nodes.OfType<TextSyntax>().FirstOrDefault(node =>
                    node.Span.Start <= contentOffset && node.Span.End >= contentOffset);
                ElementSyntax? opening = elements.FirstOrDefault(element =>
                    element.NameToken.Span.End <= contentOffset && element.OpenEndToken.Span.End >= contentOffset);
                bool structural = trimmed[0] == '<' || opening is not null ||
                    textNode?.Kind == SyntaxKind.Comment || IsDirectiveLine(textNode, trimmed);
                if (structural)
                {
                    int depth = elements.Count(element =>
                        element.Span.Start < contentOffset && element.Span.End >= contentOffset);
                    if (trimmed.StartsWith("</", StringComparison.Ordinal))
                    {
                        depth--;
                    }

                    int directiveDepth = DirectiveDepth(source, contentOffset);
                    if (directiveDepth > 0 || textNode is not null && IsDirectiveLine(textNode, trimmed))
                    {
                        depth += directiveDepth;
                        if (trimmed[0] == '}')
                        {
                            depth--;
                        }
                    }

                    formatted = options.Indent(Math.Max(0, depth)) + trimmed;
                }
            }

            changed |= !string.Equals(original, formatted, StringComparison.Ordinal);
            replacement.Append(formatted).Append(source, line.Start + line.ContentLength, line.End - line.Start - line.ContentLength);
        }

        if (!changed)
        {
            return Array.Empty<CernealaFormattingEdit>();
        }

        int editStart = lines[firstLine].Start;
        int editEnd = lines[lastLine].End;
        return [new CernealaFormattingEdit(new TextSpan(editStart, editEnd - editStart), replacement.ToString())];
    }

    private static bool IsDirectiveLine(TextSyntax? text, string trimmed)
    {
        if (text is null || text.Token.Text.IndexOf('@') < 0)
        {
            return false;
        }

        return trimmed[0] is '@' or '{' or '}' || trimmed.IndexOf('=') > 0;
    }

    private static int DirectiveDepth(string source, int offset)
    {
        int depth = 0;
        int lineStart = 0;
        while (lineStart < offset)
        {
            int lineEnd = source.IndexOfAny(['\r', '\n'], lineStart);
            if (lineEnd < 0 || lineEnd >= offset)
            {
                break;
            }

            string line = source.Substring(lineStart, lineEnd - lineStart).TrimStart(' ', '\t');
            if (line.Length > 0 && line[0] != '<' &&
                (depth > 0 || line[0] is '@' or '{' or '}'))
            {
                depth = ApplyDirectiveBraces(line, depth);
            }

            lineStart = lineEnd + 1;
            if (source[lineEnd] == '\r' && lineStart < source.Length && source[lineStart] == '\n')
            {
                lineStart++;
            }
        }

        return depth;
    }

    private static int ApplyDirectiveBraces(string line, int depth)
    {
        bool quoted = false;
        bool escaped = false;
        char quote = '\0';
        foreach (char character in line)
        {
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
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}')
            {
                depth = Math.Max(0, depth - 1);
            }
        }

        return depth;
    }

    private static TextLineInfo[] GetLines(string text)
    {
        List<TextLineInfo> lines = new();
        int start = 0;
        for (int offset = 0; offset < text.Length; offset++)
        {
            if (text[offset] is not ('\r' or '\n'))
            {
                continue;
            }

            int contentEnd = offset;
            if (text[offset] == '\r' && offset + 1 < text.Length && text[offset + 1] == '\n')
            {
                offset++;
            }

            lines.Add(new TextLineInfo(start, contentEnd - start, offset + 1));
            start = offset + 1;
        }

        if (start <= text.Length)
        {
            lines.Add(new TextLineInfo(start, text.Length - start, text.Length));
        }

        return lines.ToArray();
    }

    private static IEnumerable<SyntaxNode> DescendantNodes(SyntaxNode node)
    {
        foreach (SyntaxNode child in node switch
        {
            DocumentSyntax document => document.Children,
            ElementSyntax element => element.Children,
            _ => Array.Empty<SyntaxNode>()
        })
        {
            yield return child;
            foreach (SyntaxNode descendant in DescendantNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private readonly record struct TextLineInfo(int Start, int ContentLength, int End);
}

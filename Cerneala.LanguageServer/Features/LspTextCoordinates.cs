using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;

namespace Cerneala.LanguageServer.Features;

internal static class LspTextCoordinates
{
    // LSP 3.17 Position: a character greater than the line length defaults back to the line length,
    // and the line length excludes the line break.
    public static int ToOffset(SourceText source, LspPosition position)
    {
        int lineStart = source.GetOffset(new LinePosition(position.Line, 0));
        int lineEnd = position.Line + 1 < source.LineCount
            ? source.GetOffset(new LinePosition(position.Line + 1, 0))
            : source.Length;
        while (lineEnd > lineStart && source[lineEnd - 1] is '\r' or '\n')
        {
            lineEnd--;
        }

        return lineStart + Math.Min(position.Character, lineEnd - lineStart);
    }

    public static TextSpan ToSpan(SourceText source, LspRange range)
    {
        int start = ToOffset(source, range.Start);
        int end = ToOffset(source, range.End);
        return new TextSpan(start, end - start);
    }

    public static LspRange ToRange(SourceText source, TextSpan span)
    {
        LinePosition start = source.GetLinePosition(span.Start);
        LinePosition end = source.GetLinePosition(span.End);
        return new LspRange
        {
            Start = new LspPosition { Line = start.Line, Character = start.Character },
            End = new LspPosition { Line = end.Line, Character = end.Character }
        };
    }
}

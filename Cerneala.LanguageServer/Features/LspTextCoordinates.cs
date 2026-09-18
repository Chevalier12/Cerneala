using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;

namespace Cerneala.LanguageServer.Features;

internal static class LspTextCoordinates
{
    public static int ToOffset(SourceText source, LspPosition position) =>
        source.GetOffset(new LinePosition(position.Line, position.Character));

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

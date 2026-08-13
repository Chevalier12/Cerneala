namespace Cerneala.Language.Syntax.Embedded;

internal static class MotionSyntaxParser
{
    public static EmbeddedParseResult<DirectiveDocumentSyntax> Parse(string text, int absoluteOffset = 0) =>
        DirectiveSyntaxParser.Parse(text, absoluteOffset, EmbeddedLanguageKind.Motion);
}

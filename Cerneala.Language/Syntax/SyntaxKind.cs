namespace Cerneala.Language.Syntax;

internal enum SyntaxKind
{
    EndOfFileToken,
    BadToken,
    WhitespaceToken,
    TextToken,
    LessThanToken,
    GreaterThanToken,
    SlashToken,
    EqualsToken,
    NameToken,
    StringToken,
    CommentToken,
    CDataToken,
    ProcessingInstructionToken,
    Document,
    Element,
    PropertyElement,
    Attribute,
    Text,
    Comment,
    CData,
    Error
}

using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal sealed class CernealaDocument
{
    public CernealaDocument(string path, SourceText text)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Syntax = MarkupParser.Parse(text);
    }

    public string Path { get; }

    public SourceText Text { get; }

    public long Version => Text.Version;

    public DocumentSyntax Syntax { get; }

    public CernealaDocument WithText(SourceText text) => new(Path, text);

    public CernealaDocument WithChange(TextChange change) => WithText(Text.WithChange(change));
}

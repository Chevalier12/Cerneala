using System.Text;

namespace Cerneala.UI.Documents;

public class Span : Inline
{
    public Span()
    {
        Inlines = new InlineCollection(this);
    }

    public Span(Inline childInline)
        : this()
    {
        ArgumentNullException.ThrowIfNull(childInline);
        Inlines.Add(childInline);
    }

    public InlineCollection Inlines { get; }

    internal override void AppendText(StringBuilder builder)
    {
        foreach (Inline inline in Inlines)
        {
            inline.AppendText(builder);
        }
    }

    internal override void CollectPieces(InlineStyle inherited, List<(string Text, InlineStyle Style)> pieces)
    {
        InlineStyle style = Resolve(inherited);
        foreach (Inline inline in Inlines)
        {
            inline.CollectPieces(style, pieces);
        }
    }
}

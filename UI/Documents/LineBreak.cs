using System.Text;

namespace Cerneala.UI.Documents;

// A line break inside the text of a TextBlock. It contributes "\n" to the text of the TextBlock.
public sealed class LineBreak : Inline
{
    internal override void AppendText(StringBuilder builder)
    {
        builder.Append('\n');
    }

    internal override void CollectPieces(InlineStyle inherited, List<(string Text, InlineStyle Style)> pieces)
    {
        pieces.Add(("\n", Resolve(inherited)));
    }
}

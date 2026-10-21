using Cerneala.Drawing;

namespace Cerneala.UI.Documents;

// A Span whose FontStyle is Italic unless it is set on the Italic itself.
public sealed class Italic : Span
{
    public Italic()
    {
        SetFrameworkDefault(FontStyleProperty, FontStyle.Italic);
    }

    public Italic(Inline childInline)
        : base(childInline)
    {
        SetFrameworkDefault(FontStyleProperty, FontStyle.Italic);
    }
}

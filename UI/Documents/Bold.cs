using Cerneala.Drawing;

namespace Cerneala.UI.Documents;

// A Span whose FontWeight is Bold unless it is set on the Bold itself.
public sealed class Bold : Span
{
    public Bold()
    {
        SetFrameworkDefault(FontWeightProperty, FontWeight.Bold);
    }

    public Bold(Inline childInline)
        : base(childInline)
    {
        SetFrameworkDefault(FontWeightProperty, FontWeight.Bold);
    }
}

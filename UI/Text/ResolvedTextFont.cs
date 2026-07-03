using Cerneala.Drawing;

namespace Cerneala.UI.Text;

public sealed class ResolvedTextFont
{
    public ResolvedTextFont(IDrawFont font)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        Identity = $"{font.FamilyName}:{font.Size:R}";
    }

    public ResolvedTextFont(IDrawFont font, string identity)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("Font identity cannot be empty.", nameof(identity));
        }

        Identity = identity;
    }

    public IDrawFont Font { get; }

    public string Identity { get; }
}

using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public sealed class FontResource
{
    private readonly IDrawFont font;

    public FontResource(IDrawFont font)
    {
        this.font = font ?? throw new ArgumentNullException(nameof(font));
    }

    public IDrawFont Resolve()
    {
        return font;
    }
}

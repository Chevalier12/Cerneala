using Cerneala.Drawing;
using Cerneala.Drawing.Text;

namespace Cerneala.UI.Hosting.MonoGame;

public sealed class MonoGameContentServices
{
    public MonoGameContentServices(IFontSource? fontSource = null, SkiaTextRasterizer? textRasterizer = null)
    {
        FontSource = fontSource ?? new SystemFontSource();
        TextRasterizer = textRasterizer ?? new SkiaTextRasterizer();
    }

    public IFontSource FontSource { get; }

    public SkiaTextRasterizer TextRasterizer { get; }

    public IDrawFont LoadFont(string familyName, float size)
    {
        return FontSource.LoadFont(familyName, size);
    }
}

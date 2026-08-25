using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting;

/// <summary>
/// Owns the MonoGame-independent font, text rasterization, and image resource services used by a UI host.
/// </summary>
public class DrawingContentServices : IDisposable
{
    private bool disposed;

    public DrawingContentServices(
        IFontSource? fontSource = null,
        SkiaTextRasterizer? textRasterizer = null,
        IImageLoader? imageLoader = null)
    {
        FontSource = fontSource ?? new SystemFontSource();
        TextRasterizer = textRasterizer ?? new SkiaTextRasterizer();
        ImageLoader = imageLoader;
        ImageResourceCache = new ImageResourceCache(imageLoader);
    }

    public IFontSource FontSource { get; }

    public SkiaTextRasterizer TextRasterizer { get; }

    public IImageLoader? ImageLoader { get; }

    public ImageResourceCache ImageResourceCache { get; }

    public IDrawFont LoadFont(string familyName, float size)
    {
        return FontSource.LoadFont(familyName, size);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ImageResourceCache.Dispose();
        disposed = true;
    }
}

using SkiaSharp;

namespace Cerneala.Drawing.Skia;

public sealed class SkiaDrawImage : IDrawImage, IDisposable
{
    public SkiaDrawImage(SKImage image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public SKImage Image { get; }

    public int Width => Image.Width;

    public int Height => Image.Height;

    public void Dispose()
    {
        Image.Dispose();
    }
}

using Cerneala.Drawing;
using Cerneala.Drawing.Skia;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.UI.Hosting.Windows;

internal sealed class SkiaImageLoader : IImageLoader
{
    public IDrawImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(path));
        }

        using SKData data = SKData.Create(path) ?? throw new InvalidOperationException($"Could not read image '{path}'.");
        SKImage image = SKImage.FromEncodedData(data) ?? throw new InvalidOperationException($"Could not decode image '{path}'.");
        return new SkiaDrawImage(image);
    }
}

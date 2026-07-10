using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Skia;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using SkiaSharp;

namespace Cerneala.Tests.Drawing;

public sealed class SkiaDrawingBackendTests
{
    [Fact]
    public void RendersAllCommandKindsAndProducesNonWhitePixels()
    {
        using SkiaDrawingBackend backend = new(64, 64, 1);
        using SKTypeface typeface = SKTypeface.FromFamilyName("Arial");
        SkiaFont font = new(typeface, "Arial", 12);
        using SKBitmap imageBitmap = new(2, 2, SKColorType.Bgra8888, SKAlphaType.Premul);
        imageBitmap.Erase(SKColors.Magenta);
        using SKImage image = SKImage.FromBitmap(imageBitmap);
        using SkiaDrawImage drawImage = new(image);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 16), new DrawColor(255, 0, 0)));
        commands.Add(DrawCommand.DrawRectangle(new DrawRect(18, 0, 16, 16), new DrawColor(0, 0, 255), 2));
        commands.Add(DrawCommand.FillEllipse(new DrawRect(36, 0, 16, 16), new DrawColor(0, 255, 0)));
        commands.Add(DrawCommand.DrawEllipse(new DrawRect(0, 18, 16, 16), new DrawColor(255, 0, 255), 2));
        commands.Add(DrawCommand.DrawLine(new DrawPoint(18, 18), new DrawPoint(32, 32), new DrawColor(0, 0, 0), 2));
        commands.Add(DrawCommand.DrawText(new DrawTextRun(font, "Text", 12), new DrawPoint(0, 38), new DrawColor(0, 0, 0)));
        commands.Add(DrawCommand.DrawImage(drawImage, new DrawRect(36, 18, 16, 16), new DrawColor(255, 255, 255)));
        commands.Add(DrawCommand.PushClip(new DrawRect(48, 48, 8, 8)));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(40, 40, 24, 24), new DrawColor(0, 0, 0)));
        commands.Add(DrawCommand.PopClip());

        backend.Render(commands);

        byte[] pixels = new byte[backend.RowBytes * backend.PixelHeight];
        Marshal.Copy(backend.Pixels, pixels, 0, pixels.Length);
        Assert.Contains(pixels, value => value != 255);
        int redPixel = (4 * backend.RowBytes) + (4 * 4);
        Assert.Equal(0, pixels[redPixel]);
        Assert.Equal(0, pixels[redPixel + 1]);
        Assert.Equal(255, pixels[redPixel + 2]);
        Assert.Equal(255, pixels[redPixel + 3]);
    }

    [Fact]
    public void RejectsImagesFromAnotherDrawingBackend()
    {
        using SkiaDrawingBackend backend = new(16, 16, 1);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawImage(new ForeignImage(), new DrawRect(0, 0, 8, 8), new DrawColor(255, 255, 255)));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => backend.Render(commands));

        Assert.Contains("SkiaDrawImage", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersBackendNeutralFontsProducedByControls()
    {
        using SkiaDrawingBackend backend = new(64, 32, 1);
        ControlTextFont font = new("Segoe UI", 14);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(
            new DrawTextRun(font, "Window text", 14),
            new DrawPoint(0, 0),
            DrawColor.Black));

        backend.Render(commands);

        byte[] pixels = new byte[backend.RowBytes * backend.PixelHeight];
        Marshal.Copy(backend.Pixels, pixels, 0, pixels.Length);
        Assert.Contains(pixels, value => value != 255);
    }

    private sealed class ForeignImage : IDrawImage
    {
        public int Width => 1;

        public int Height => 1;
    }
}

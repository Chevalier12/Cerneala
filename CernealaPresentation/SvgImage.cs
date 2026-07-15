using System.Globalization;
using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using SkiaSharp;

namespace Cerneala.Presentation;

public sealed class SvgImage : Image
{
    private IDrawImage? loadedImage;

    private const string SourcePath = "Assets/cerneala-mascot-void-suction-well.svg";

    public float DisplaySize { get; set; } = 250;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(DisplaySize, DisplaySize);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (loadedImage is not null)
        {
            return;
        }

        string svgPath = ResolveAssetPath(SourcePath);
        try
        {
            string pngPath = RasterizePixelSvg(svgPath);
            loadedImage = Root?.ImageLoader?.Load(pngPath);
        }
        catch when (File.Exists(Path.ChangeExtension(svgPath, ".png")))
        {
            loadedImage = Root?.ImageLoader?.Load(Path.ChangeExtension(svgPath, ".png"));
        }

        Source = loadedImage;
    }

    protected override void OnDetached()
    {
        Source = null;
        (loadedImage as IDisposable)?.Dispose();
        loadedImage = null;
        base.OnDetached();
    }

    private static string ResolveAssetPath(string path)
    {
        string outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        return File.Exists(outputPath) ? outputPath : Path.GetFullPath(path);
    }

    private static string RasterizePixelSvg(string svgPath)
    {
        long version = File.GetLastWriteTimeUtc(svgPath).Ticks;
        string cacheDirectory = Path.Combine(Path.GetTempPath(), "CernealaPresentation");
        string cachePath = Path.Combine(cacheDirectory, $"mascot-{version}.png");
        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        XDocument document = XDocument.Load(svgPath);
        XElement root = document.Root ?? throw new InvalidDataException("SVG document has no root element.");
        float[] viewBox = ParseNumbers((string?)root.Attribute("viewBox") ?? "0 0 1254 1254");
        int width = (int)MathF.Ceiling(viewBox[2]);
        int height = (int)MathF.Ceiling(viewBox[3]);

        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint paint = new() { IsAntialias = false, Style = SKPaintStyle.Fill };

        foreach (XElement rect in root.Descendants().Where(element => element.Name.LocalName == "rect"))
        {
            string? fill = rect.AncestorsAndSelf()
                .Select(element => (string?)element.Attribute("fill"))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (fill is null || fill.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            paint.Color = SKColor.Parse(fill);
            float x = ParseNumber((string?)rect.Attribute("x"));
            float y = ParseNumber((string?)rect.Attribute("y"));
            float rectWidth = ParseNumber((string?)rect.Attribute("width"));
            float rectHeight = ParseNumber((string?)rect.Attribute("height"));
            canvas.DrawRect(x - viewBox[0], y - viewBox[1], rectWidth, rectHeight, paint);
        }

        Directory.CreateDirectory(cacheDirectory);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream output = File.Create(cachePath);
        data.SaveTo(output);
        return cachePath;
    }

    private static float[] ParseNumbers(string value)
    {
        return value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => float.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static float ParseNumber(string? value)
    {
        return float.Parse(value ?? "0", CultureInfo.InvariantCulture);
    }
}

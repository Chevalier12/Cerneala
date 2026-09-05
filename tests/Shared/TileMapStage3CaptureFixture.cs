using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.SmokeTests;

internal static class TileMapStage3CaptureFixture
{
    internal static RenderSurface2D CreateSurface(string artifactDirectory)
    {
        string atlasPath = Path.Combine(artifactDirectory, "tilemap-atlas.png");
        using (SKBitmap bitmap = new(new SKImageInfo(
            2,
            1,
            SKColorType.Rgba8888,
            SKAlphaType.Premul)))
        {
            bitmap.SetPixel(0, 0, new SKColor(32, 200, 80));
            bitmap.SetPixel(1, 0, new SKColor(80, 240, 140));
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }

        ResourceId<ImageResource> atlasId = new("TileMapStage3Atlas");
        TileSet2D tileSet = new(
            "Stage3",
            atlasId,
            [
                new TileDefinition2D(1, new DrawRect(0, 0, 1, 1)),
                new TileDefinition2D(2, new DrawRect(1, 0, 1, 1))
            ]);
        TileChunk2D[] chunks = Enumerable.Range(-1, 7)
            .Select(static x => new TileChunk2D(
                new TileCoordinate2D(x, 0),
                1,
                1,
                [new TileCell2D((x & 1) == 0 ? 1 : 2)]))
            .ToArray();
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [tileSet],
                [new TileLayer2DModel("Boundary", chunks)])
        };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 64, 16)
        };
        surface.Resources.SetResource(atlasId, new ImageResource(atlasPath));
        return surface;
    }

    internal static void VerifyPanCaptures(
        string beforePath,
        string afterPath,
        string backend)
    {
        SKColor beforeFirstPixel = VerifyCapture(beforePath, backend);
        SKColor afterFirstPixel = VerifyCapture(afterPath, backend);
        if (beforeFirstPixel == afterFirstPixel)
        {
            throw new InvalidOperationException(
                $"{backend} TileMap2D pan did not shift the alternating boundary fixture.");
        }
    }

    private static SKColor VerifyCapture(string path, string backend)
    {
        using SKBitmap bitmap = SKBitmap.Decode(path) ??
            throw new InvalidOperationException(
                $"{backend} could not decode its TileMap2D capture '{path}'.");
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Red >= 100 || pixel.Green <= 180 ||
                    pixel.Blue is < 60 or > 160 || pixel.Alpha <= 240)
                {
                    throw new InvalidOperationException(
                        $"{backend} TileMap2D capture has a missing/incorrect edge pixel at ({x},{y}): {pixel}.");
                }
            }
        }

        int middleY = bitmap.Height / 2;
        int transitions = 0;
        for (int x = 1; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x - 1, middleY) != bitmap.GetPixel(x, middleY))
            {
                transitions++;
            }
        }
        if (transitions != 3)
        {
            throw new InvalidOperationException(
                $"{backend} TileMap2D capture expected 3 tile boundaries but found {transitions} in '{path}'.");
        }
        return bitmap.GetPixel(0, middleY);
    }
}

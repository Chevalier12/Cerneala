using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.SceneVillage;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using SkiaSharp;
using Xunit.Abstractions;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SceneVillage;

[Collection(VillageNativeTestCollection.Name)]
public sealed class NativeVillageScaleTests
{
    private readonly ITestOutputHelper output;

    public NativeVillageScaleTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void VillageUsesNativeArtworkPixelsAtThreeWindowScalesWithoutDownsamplingItsSurface()
    {
        string captureDirectory = CreateCaptureDirectory();
        SKColor[]? referenceTile = null;
        SKColor[]? referencePlayer = null;
        using SKBitmap townAtlas = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "Assets", "tiny-town.png"))
            ?? throw new InvalidOperationException("The included town atlas could not be decoded.");
        using SKBitmap characterAtlas = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "Assets", "ch003.png"))
            ?? throw new InvalidOperationException("The included character atlas could not be decoded.");
        // The 821-DIP case also exercises a non-default fractional-DPI resize.
        foreach ((float requestedDpi, float windowWidth) in new[] { (1f, 820f), (1.25f, 821f), (2f, 820f) })
        {
            NativeSdlApi api = new();
            using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
            using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: requestedDpi);
            using WindowApplicationRuntime runtime = new(platform);
            var window = new MainWindow { Width = windowWidth, Height = 600 };
            try
            {
                runtime.Show(window, modal: false);
                VillageGameSurface surface = Assert.Single(DescendantsAndSelf(window).OfType<VillageGameSurface>());
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready &&
                           surface.ArrangedBounds.Width > 0f && surface.ArrangedBounds.Height > 0f);
                UiViewport viewport = Assert.IsType<UiFrame>(window.LastFrame).Viewport;
                Assert.Equal(requestedDpi, viewport.Scale);
                Assert.Equal(1f, surface.ContentScale);
                Assert.Equal(1f, surface.Zoom);
                Assert.Equal(DrawBrushStretch.Fill, surface.Stretch);

                int targetWidth = Math.Max(1, checked((int)MathF.Ceiling(surface.ArrangedBounds.Width * viewport.Scale)));
                int targetHeight = Math.Max(1, checked((int)MathF.Ceiling(surface.ArrangedBounds.Height * viewport.Scale)));
                Assert.Equal(targetWidth, surface.ViewBox!.Value.Width);
                Assert.Equal(targetHeight, surface.ViewBox.Value.Height);

                Sprite2D tile = Assert.Single(surface.Scene!.Children.OfType<Sprite2D>()
                    .Where(sprite => sprite.X == 2000f && sprite.Y == 2000f &&
                                     sprite.SourceY == 0f && sprite.SourceWidth == 16f && sprite.SourceHeight == 16f));
                Sprite2D player = Assert.Single(surface.Scene.Children.OfType<Sprite2D>()
                    .Where(sprite => sprite.Collider?.IsSimulated == true));
                Assert.Equal((16f, 16f, 16f, 16f), (tile.SourceWidth, tile.SourceHeight, tile.Width, tile.Height));
                Assert.Equal((32f, 32f, 32f, 32f), (player.SourceWidth, player.SourceHeight, player.Width, player.Height));

                Vector2 tileRoot = surface.SceneToRoot(new Vector2(tile.X, tile.Y));
                Vector2 tileFarRoot = surface.SceneToRoot(new Vector2(tile.X + tile.Width, tile.Y + tile.Height));
                float left = tileRoot.X * viewport.Scale;
                float top = tileRoot.Y * viewport.Scale;
                float width = (tileFarRoot.X - tileRoot.X) * viewport.Scale;
                float height = (tileFarRoot.Y - tileRoot.Y) * viewport.Scale;
                Assert.InRange(MathF.Abs(left - MathF.Round(left)), 0f, 0.01f);
                Assert.InRange(MathF.Abs(top - MathF.Round(top)), 0f, 0.01f);
                Assert.InRange(MathF.Abs(width - 16f), 0f, 0.01f);
                Assert.InRange(MathF.Abs(height - 16f), 0f, 0.01f);
                Vector2 playerRoot = surface.SceneToRoot(new Vector2(player.X, player.Y));
                Vector2 playerFarRoot = surface.SceneToRoot(new Vector2(player.X + player.Width, player.Y + player.Height));
                float playerLeft = playerRoot.X * viewport.Scale;
                float playerTop = playerRoot.Y * viewport.Scale;
                Assert.InRange(MathF.Abs(playerLeft - MathF.Round(playerLeft)), 0f, 0.01f);
                Assert.InRange(MathF.Abs(playerTop - MathF.Round(playerTop)), 0f, 0.01f);
                Assert.InRange(MathF.Abs((playerFarRoot.X - playerRoot.X) * viewport.Scale - 32f), 0f, 0.01f);
                Assert.InRange(MathF.Abs((playerFarRoot.Y - playerRoot.Y) * viewport.Scale - 32f), 0f, 0.01f);

                string basePath = Path.Combine(captureDirectory, $"village-dpi-{requestedDpi:0.##}-native.png");
                runtime.PumpOnce(TimeSpan.Zero);
                window.SaveScreenshot(basePath);
                using SKBitmap bitmap = SKBitmap.Decode(basePath)
                    ?? throw new InvalidOperationException("App screenshot could not be decoded.");
                Assert.Equal(checked((int)MathF.Ceiling(viewport.Width * viewport.Scale)), bitmap.Width);
                Assert.Equal(checked((int)MathF.Ceiling(viewport.Height * viewport.Scale)), bitmap.Height);
                int tileLeft = checked((int)MathF.Round(left));
                int tileTop = checked((int)MathF.Round(top));
                Assert.InRange(tileLeft, 0, bitmap.Width - 16);
                Assert.InRange(tileTop, 0, bitmap.Height - 16);
                SKColor[] tilePixels = Crop(bitmap, tileLeft, tileTop, 16);
                (int exactTownTexels, string townOpaqueBounds) = AssertOpaqueSourceTexelsMatchScreen(
                    townAtlas, bitmap, checked((int)tile.SourceX), checked((int)tile.SourceY), tileLeft, tileTop, 16);
                Assert.True(exactTownTexels >= 128, $"Expected substantial opaque native town artwork; found {exactTownTexels} texels.");
                if (referenceTile is null)
                {
                    referenceTile = tilePixels;
                }
                else
                {
                    Assert.Equal(referenceTile, tilePixels);
                }
                int playerPixelLeft = checked((int)MathF.Round(playerLeft));
                int playerPixelTop = checked((int)MathF.Round(playerTop));
                SKColor[] playerPixels = Crop(bitmap, playerPixelLeft, playerPixelTop, 32);
                (int exactCharacterTexels, string characterOpaqueBounds) = AssertOpaqueSourceTexelsMatchScreen(
                    characterAtlas, bitmap, checked((int)player.SourceX), checked((int)player.SourceY), playerPixelLeft, playerPixelTop, 32);
                Assert.True(exactCharacterTexels >= 64,
                    $"Expected substantial opaque native character artwork; found {exactCharacterTexels} texels.");
                if (referencePlayer is null)
                {
                    referencePlayer = playerPixels;
                }
                else
                {
                    Assert.Equal(referencePlayer, playerPixels);
                }

                var initialBounds = surface.ArrangedBounds;
                surface.Zoom = 2f;
                Assert.Equal(targetWidth / 2f, surface.ViewBox!.Value.Width);
                Assert.Equal(targetHeight / 2f, surface.ViewBox.Value.Height);
                surface.ContentScale = null;
                Assert.Equal(targetWidth / (viewport.Scale * 2f), surface.ViewBox!.Value.Width);
                Assert.Equal(targetHeight / (viewport.Scale * 2f), surface.ViewBox.Value.Height);
                Vector2 inheritedNear = surface.SceneToRoot(new Vector2(tile.X, tile.Y));
                Vector2 inheritedFar = surface.SceneToRoot(new Vector2(tile.X + 16f, tile.Y));
                Assert.InRange(MathF.Abs((inheritedFar.X - inheritedNear.X) * viewport.Scale - 32f * viewport.Scale), 0f, 0.02f);
                Assert.Equal(initialBounds, surface.ArrangedBounds);
                Assert.Equal(requestedDpi, Assert.IsType<UiFrame>(window.LastFrame).Viewport.Scale);

                surface.ContentScale = 1f;
                Vector2 zoomNear = surface.SceneToRoot(new Vector2(tile.X, tile.Y));
                Vector2 zoomFar = surface.SceneToRoot(new Vector2(tile.X + 16f, tile.Y));
                Assert.InRange(MathF.Abs((zoomFar.X - zoomNear.X) * viewport.Scale - 32f), 0f, 0.01f);
                string zoomPath = Path.Combine(captureDirectory, $"village-dpi-{requestedDpi:0.##}-zoom2.png");
                runtime.PumpOnce(TimeSpan.Zero);
                window.SaveScreenshot(zoomPath);
                using SKBitmap zoomBitmap = SKBitmap.Decode(zoomPath)
                    ?? throw new InvalidOperationException("Zoomed app screenshot could not be decoded.");
                Assert.Equal(bitmap.Width, zoomBitmap.Width);
                Assert.Equal(bitmap.Height, zoomBitmap.Height);
                Assert.True(CountDifferentPixels(bitmap, zoomBitmap) > 100,
                    "Changing the camera ViewBox must change the captured scene without changing target dimensions.");

                // Zoom is programmatic; input remains a routed, user-like path.
                ServoApi.SetId(surface, "village-scale-surface");
                ServoApi servo = new(window);
                Run(servo.ClickAsync(ServoTarget.ById("village-scale-surface")));
                float oldX = surface.PlayerPosition.X;
                Run(servo.PressKeyAsync(InputKey.D));
                Wait(() => surface.PlayerPosition.X > oldX);
                Vector2 movedNear = surface.SceneToRoot(new Vector2(tile.X, tile.Y));
                Vector2 movedFar = surface.SceneToRoot(new Vector2(tile.X + 16f, tile.Y));
                float movedPixelWidth = (movedFar.X - movedNear.X) * viewport.Scale;
                Assert.InRange(MathF.Abs(movedPixelWidth - 32f), 0f, 0.01f);
                string movedPath = Path.Combine(captureDirectory, $"village-dpi-{requestedDpi:0.##}-after-servo.png");
                runtime.PumpOnce(TimeSpan.Zero);
                window.SaveScreenshot(movedPath);
                using SKBitmap movedBitmap = SKBitmap.Decode(movedPath)
                    ?? throw new InvalidOperationException("Moved app screenshot could not be decoded.");
                Assert.Equal(bitmap.Width, movedBitmap.Width);
                Assert.Equal(bitmap.Height, movedBitmap.Height);
                output.WriteLine($"DPI={requestedDpi}; windowWidth={windowWidth}; surfaceDip={surface.ArrangedBounds.Width}x{surface.ArrangedBounds.Height}; " +
                    $"target={targetWidth}x{targetHeight}; screenshot={bitmap.Width}x{bitmap.Height}; " +
                    $"native town=16px ({exactTownTexels} exact opaque source texels, bounds {townOpaqueBounds}), " +
                    $"actor=32px ({exactCharacterTexels} exact opaque source texels, bounds {characterOpaqueBounds}); " +
                    $"zoom2 town={movedPixelWidth:F3}px; " +
                    $"cameraX={surface.ViewBox!.Value.X:F4}; captures={basePath}, {zoomPath}, {movedPath}");

                void Run(Task action)
                {
                    Wait(() => action.IsCompleted);
                    Assert.True(action.IsCompletedSuccessfully, action.Exception?.ToString());
                }

                void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
                {
                    runtime.PumpOnce(TimeSpan.Zero);
                    return done();
                }, TimeSpan.FromSeconds(15)), $"Village native scale {requestedDpi} did not reach its required state.");
            }
            finally
            {
                runtime.Close(window, force: true);
            }
        }
    }

    private static SKColor[] Crop(SKBitmap bitmap, int left, int top, int size)
    {
        Assert.InRange(left, 0, bitmap.Width - size);
        Assert.InRange(top, 0, bitmap.Height - size);
        return Enumerable.Range(0, size)
            .SelectMany(y => Enumerable.Range(0, size).Select(x => bitmap.GetPixel(left + x, top + y)))
            .ToArray();
    }

    private static (int Count, string Bounds) AssertOpaqueSourceTexelsMatchScreen(
        SKBitmap source, SKBitmap screen, int sourceX, int sourceY, int screenX, int screenY, int size)
    {
        Assert.InRange(sourceX, 0, source.Width - size);
        Assert.InRange(sourceY, 0, source.Height - size);
        Assert.InRange(screenX, 0, screen.Width - size);
        Assert.InRange(screenY, 0, screen.Height - size);
        int opaqueTexels = 0;
        int minX = size;
        int minY = size;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                SKColor expected = source.GetPixel(sourceX + x, sourceY + y);
                if (expected.Alpha != byte.MaxValue)
                {
                    continue;
                }

                opaqueTexels++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                SKColor actual = screen.GetPixel(screenX + x, screenY + y);
                Assert.True(actual == expected,
                    $"Native source texel ({sourceX + x},{sourceY + y}) differs at screen ({screenX + x},{screenY + y}): " +
                    $"expected {expected}, actual {actual}.");
            }
        }

        return (opaqueTexels, $"({minX},{minY})..({maxX},{maxY})");
    }

    private static int CountDifferentPixels(SKBitmap first, SKBitmap second)
    {
        int different = 0;
        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                {
                    different++;
                }
            }
        }

        return different;
    }

    private static string CreateCaptureDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(directory.FullName, "artifacts", "ci", "scene-village", "screenshots",
            "native-scale-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current))
            {
                yield break;
            }

            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}

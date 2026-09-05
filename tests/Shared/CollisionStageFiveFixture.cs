using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.SmokeTests;

internal sealed class CollisionStageFiveFixture
{
    internal const string ClosedCaptureName = "collision-closed.png";
    internal const string OpenCaptureName = "collision-open.png";

    private readonly Sprite2D closedDoor;
    private readonly Sprite2D openDoor;
    private readonly BoxCollider2D doorCollider;
    private readonly CircleCollider2D playerCollider;

    private CollisionStageFiveFixture(
        RenderSurface2D surface,
        Scene2D scene,
        Sprite2D closedDoor,
        Sprite2D openDoor,
        BoxCollider2D doorCollider,
        CircleCollider2D playerCollider)
    {
        Surface = surface;
        Scene = scene;
        this.closedDoor = closedDoor;
        this.openDoor = openDoor;
        this.doorCollider = doorCollider;
        this.playerCollider = playerCollider;
    }

    internal RenderSurface2D Surface { get; }

    internal Scene2D Scene { get; }

    internal static CollisionStageFiveFixture Create(string artifactDirectory)
    {
        string fullDirectory = Path.GetFullPath(artifactDirectory);
        Directory.CreateDirectory(fullDirectory);
        string atlasPath = Path.Combine(fullDirectory, "collision-atlas.png");
        WriteAtlas(atlasPath);

        ResourceId<ImageResource> atlasId = new("CollisionStageFiveAtlas");
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        Scene2D house = new()
        {
            Layer = 1,
            TranslateX = 24,
            TranslateY = 8
        };
        house.Children.Add(CreateSprite(atlasId, 0, new DrawRect(0, 0, 80, 56)));

        AddWall(house, atlasId, new DrawRect(0, 0, 80, 8));
        AddWall(house, atlasId, new DrawRect(0, 8, 8, 48));
        AddWall(house, atlasId, new DrawRect(72, 8, 8, 48));
        AddWall(house, atlasId, new DrawRect(0, 48, 30, 8));
        AddWall(house, atlasId, new DrawRect(50, 48, 30, 8));

        Scene2D door = new()
        {
            Layer = 2,
            TranslateX = 30,
            TranslateY = 48
        };
        Sprite2D closedDoor = CreateSprite(
            atlasId,
            2,
            new DrawRect(0, 0, 20, 8));
        Sprite2D openDoor = CreateSprite(
            atlasId,
            2,
            new DrawRect(18, -12, 4, 20));
        openDoor.IsVisible = false;
        BoxCollider2D doorCollider = new()
        {
            Width = 20,
            Height = 8,
            CollisionLayer = 2,
            CollisionMask = 1
        };
        door.Children.Add(closedDoor);
        door.Children.Add(openDoor);
        door.Children.Add(doorCollider);
        house.Children.Add(door);
        scene.Children.Add(house);

        Scene2D player = new()
        {
            Layer = 3,
            TranslateX = 60,
            TranslateY = 68
        };
        player.Children.Add(CreateSprite(atlasId, 3, new DrawRect(0, 0, 8, 8)));
        CircleCollider2D playerCollider = new()
        {
            Radius = 4,
            OffsetX = 4,
            OffsetY = 4,
            CollisionLayer = 1,
            CollisionMask = 2
        };
        player.Children.Add(playerCollider);
        scene.Children.Add(player);

        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 128, 80),
            Stretch = DrawBrushStretch.Fill,
            ClearColor = new Color(12, 20, 30)
        };
        surface.Resources.SetResource(atlasId, new ImageResource(atlasPath));
        return new CollisionStageFiveFixture(
            surface,
            scene,
            closedDoor,
            openDoor,
            doorCollider,
            playerCollider);
    }

    internal void VerifyClosedContract()
    {
        MoveCollisionResult2D result = Scene.CollisionWorld.MoveAndCollide(
            playerCollider,
            new Vector2(0, -16));
        if (!ReferenceEquals(result.Collision?.Collider, doorCollider) ||
            result.Travel.Y >= 0 ||
            result.Travel.Y <= -16)
        {
            throw new InvalidOperationException(
                "Closed-door collision did not stop the player at the door.");
        }

        CollisionHit2D? firstRayHit = Scene.CollisionWorld.Raycast(
            new Vector2(64, 76),
            -Vector2.UnitY,
            24,
            new CollisionQuery2D(exclude: playerCollider)).FirstOrDefault();
        if (!ReferenceEquals(firstRayHit?.Collider, doorCollider))
        {
            throw new InvalidOperationException(
                "Closed-door raycast did not return the door first.");
        }
    }

    internal void VerifyCoordinateRoundTrip()
    {
        Vector2 expected = new(64.25f, 59.75f);
        Vector2 rootPosition = Surface.SceneToRoot(expected);
        if (!Surface.TryRootToScene(rootPosition, out Vector2 actual) ||
            Vector2.Distance(expected, actual) > 0.001f)
        {
            throw new InvalidOperationException(
                $"Scene/root coordinate round trip failed: expected {expected}, received {actual}.");
        }
    }

    internal void OpenDoor()
    {
        doorCollider.Enabled = false;
        closedDoor.IsVisible = false;
        openDoor.IsVisible = true;
    }

    internal void VerifyOpenContract()
    {
        MoveCollisionResult2D result = Scene.CollisionWorld.MoveAndCollide(
            playerCollider,
            new Vector2(0, -16));
        if (result.Collision is not null || result.Travel != new Vector2(0, -16))
        {
            throw new InvalidOperationException(
                "Open-door collision did not allow the complete requested travel.");
        }
    }

    internal static void VerifyCaptures(string artifactDirectory, string backend)
    {
        string fullDirectory = Path.GetFullPath(artifactDirectory);
        string closedPath = Path.Combine(fullDirectory, ClosedCaptureName);
        string openPath = Path.Combine(fullDirectory, OpenCaptureName);
        using SKBitmap closed = Decode(closedPath, backend);
        using SKBitmap open = Decode(openPath, backend);
        if (closed.Width != open.Width || closed.Height != open.Height)
        {
            throw new InvalidOperationException(
                $"{backend} collision captures have different sizes.");
        }

        if (closed.Width < 320 || closed.Height < 200)
        {
            throw new InvalidOperationException(
                $"{backend} collision capture is unexpectedly small: {closed.Width}x{closed.Height}.");
        }

        int distinctClosedColors = CountDistinctColors(closed);
        if (distinctClosedColors < 5)
        {
            throw new InvalidOperationException(
                $"{backend} collision scene produced only {distinctClosedColors} colors.");
        }

        long changedPixels = CountDifferentPixels(closed, open, tolerance: 3);
        if (changedPixels < 100)
        {
            throw new InvalidOperationException(
                $"{backend} door state changed only {changedPixels} captured pixels.");
        }

        File.WriteAllText(
            Path.Combine(fullDirectory, "collision-backend.json"),
            JsonSerializer.Serialize(
                new
                {
                    Backend = backend,
                    Width = closed.Width,
                    Height = closed.Height,
                    DistinctClosedColors = distinctClosedColors,
                    ChangedPixels = changedPixels,
                    ClosedSha256 = Hash(closedPath),
                    OpenSha256 = Hash(openPath),
                    CollisionContract = "PASS",
                    CoordinateRoundTrip = "PASS"
                },
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void AddWall(
        Scene2D house,
        ResourceId<ImageResource> atlasId,
        DrawRect bounds)
    {
        house.Children.Add(CreateSprite(atlasId, 1, bounds));
        house.Children.Add(new BoxCollider2D
        {
            Width = bounds.Width,
            Height = bounds.Height,
            OffsetX = bounds.X,
            OffsetY = bounds.Y,
            CollisionLayer = 2,
            CollisionMask = 1
        });
    }

    private static Sprite2D CreateSprite(
        ResourceId<ImageResource> atlasId,
        int sourceX,
        DrawRect destination) =>
        new()
        {
            SourceResourceId = atlasId,
            SourceRect = new DrawRect(sourceX, 0, 1, 1),
            Destination = destination
        };

    private static void WriteAtlas(string path)
    {
        SKColor[] colors =
        [
            new SKColor(44, 62, 78),
            new SKColor(176, 196, 214),
            new SKColor(224, 142, 44),
            new SKColor(68, 220, 112)
        ];
        using SKBitmap bitmap = new(new SKImageInfo(
            colors.Length,
            1,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        for (int index = 0; index < colors.Length; index++)
        {
            bitmap.SetPixel(index, 0, colors[index]);
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, encoded.ToArray());
    }

    private static SKBitmap Decode(string path, string backend) =>
        SKBitmap.Decode(path) ?? throw new InvalidOperationException(
            $"{backend} could not decode collision capture '{path}'.");

    private static int CountDistinctColors(SKBitmap bitmap)
    {
        HashSet<SKColor> colors = [];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                colors.Add(bitmap.GetPixel(x, y));
            }
        }

        return colors.Count;
    }

    private static long CountDifferentPixels(
        SKBitmap left,
        SKBitmap right,
        int tolerance)
    {
        long changed = 0;
        for (int y = 0; y < left.Height; y++)
        {
            for (int x = 0; x < left.Width; x++)
            {
                SKColor a = left.GetPixel(x, y);
                SKColor b = right.GetPixel(x, y);
                int delta = Math.Max(
                    Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green)),
                    Math.Max(Math.Abs(a.Blue - b.Blue), Math.Abs(a.Alpha - b.Alpha)));
                if (delta > tolerance)
                {
                    changed++;
                }
            }
        }

        return changed;
    }

    private static string Hash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}

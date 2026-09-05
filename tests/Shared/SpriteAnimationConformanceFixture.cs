using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using SkiaSharp;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.SmokeTests;

internal sealed class SpriteAnimationConformanceFixture : IDisposable
{
    internal static readonly string[] CaptureNames =
        ["animation-000.png", "animation-100.png", "animation-200.png", "animation-300.png"];
    private readonly List<IDisposable> effects = [];
    private readonly Sprite2D sprite;
    private readonly Sprite2D reference;
    private readonly Sprite2D grouped;
    private readonly Scene2D entity;
    private readonly Scene2D prismGroup;
    private readonly TileInstance2D tile;
    private readonly BoxCollider2D collider;
    private readonly TileMap2D map;
    private bool prepared;

    internal RenderSurface2D Surface { get; }

    internal SpriteAnimationConformanceFixture(string directory)
    {
        Directory.CreateDirectory(Path.GetFullPath(directory));
        string path = Path.GetFullPath(Path.Combine(directory, "animation-atlas.png"));
        WriteAtlas(path);
        ResourceId<ImageResource> atlas = new("SpriteAnimationAtlas");
        SpriteAnimationSet clips = new([new SpriteAnimationClip("Walk", [
            new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.Horizontal),
            new SpriteAnimationFrame(new DrawRect(32, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.Vertical)])]);
        Sprite2D CreateSprite(DrawRect destination) => new()
        {
            SourceResourceId = atlas, Destination = destination, IsAnimationPaused = true,
            Aspect = new ElementAspect([
                new ElementAspectValue(Sprite2D.AnimationsProperty, clips),
                new ElementAspectValue(Sprite2D.AnimationStateProperty, "Walk"),
                new ElementAspectValue(Sprite2D.TintProperty, new Color(240, 200, 160))])
        };
        sprite = CreateSprite(new DrawRect(0, 0, 16, 16));
        sprite.Flip = RenderSurface2DSpriteFlip.Vertical;
        entity = new Scene2D { TranslateX = 8, TranslateY = 8, Layer = 2 };
        collider = new BoxCollider2D { Width = 16, Height = 16 };
        entity.Children.Add(sprite);
        entity.Children.Add(collider);
        reference = CreateSprite(new DrawRect(8, 48, 16, 16));
        grouped = CreateSprite(new DrawRect(0, 0, 16, 16));
        prismGroup = new Scene2D { TranslateX = 36, TranslateY = 8, Scale = 1.25f };
        prismGroup.Children.Add(grouped);
        map = new TileMap2D
        {
            TranslateX = 48, TranslateY = 40,
            Model = new TileMap2DModel(new DrawSize(8, 8),
                [new TileSet2D("Atlas", atlas, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                [new TileLayer2DModel("Ground", [new TileChunk2D(new TileCoordinate2D(0, 0), 8, 4,
                    Enumerable.Repeat(new TileCell2D(1), 32))])], new TileMapBounds2D(0, 0, 8, 4))
        };
        tile = map.Promote(new TileCellKey2D("Ground", 3, 1));
        tile.IsAnimationPaused = true;
        tile.Flip = TileFlip2D.Horizontal;
        tile.Aspect = new ElementAspect([
            new ElementAspectValue(TileInstance2D.AnimationsProperty, clips),
            new ElementAspectValue(TileInstance2D.AnimationStateProperty, "Walk"),
            new ElementAspectValue(TileInstance2D.TintProperty, new Color(180, 240, 200))]);
        tile.Colliders.Add(new BoxCollider2D { Width = 8, Height = 8 });
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(map);
        scene.Children.Add(prismGroup);
        scene.Children.Add(reference);
        scene.Children.Add(entity);
        Surface = new RenderSurface2D
        {
            Scene = scene, ViewBox = new DrawRect(0, 0, 128, 84),
            ClearColor = new Color(8, 16, 24)
        };
        Surface.Resources.SetResource(atlas, new ImageResource(path));
    }

    internal void Prepare()
    {
        if (prepared) return;
        prepared = true;
        VerifyGeometry();
        foreach (UIElement target in new UIElement[] { sprite, prismGroup, tile })
            effects.Add(GeneratedMarkup.AttachPrism(target, () => new PrismInstance(
                new PrismCompositionDefinition("Animated", [new PrismLayerDefinition(new PrismNodeId(1), "Content",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])]))));
        VerifyGeometry();
    }

    internal void Advance(int sample)
    {
        if (sample is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(sample));
        // Freeze between native captures, then pulse the actual surface clock
        // by an exact delta. Screenshot timing cannot change the selected frame.
        sprite.IsAnimationPaused = reference.IsAnimationPaused = grouped.IsAnimationPaused = false;
        tile.IsAnimationPaused = false;
        ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(100));
        sprite.IsAnimationPaused = reference.IsAnimationPaused = grouped.IsAnimationPaused = true;
        tile.IsAnimationPaused = true;
        if (sample == 1)
        {
            // Explicit completion exercises Motion's normal property commit at
            // a deterministic endpoint, independently of native wall-clock speed.
            sprite.Motion().Animate(UIElement.OpacityProperty).To(0.6f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200))).Complete();
            tile.Motion().Animate(UIElement.OpacityProperty).To(0.7f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200))).Complete();
            tile.Motion().Animate(UIElement.RotationProperty).To(0.2f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200))).Complete();
        }
    }

    internal void VerifySample(int sample)
    {
        VerifyGeometry();
        if (sample > 0 && (Math.Abs(sprite.Opacity - 0.6f) > 0.001f || Math.Abs(tile.Opacity - 0.7f) > 0.001f))
            throw new InvalidOperationException("Motion did not commit its endpoint.");
    }

    private void VerifyGeometry()
    {
        Scene2D scene = Surface.Scene!;
        if (!scene.CollisionWorld.Raycast(new Vector2(12, 0), Vector2.UnitY, 40)
            .Any(hit => ReferenceEquals(hit.Collider, collider) && Math.Abs(hit.Distance - 8) < 0.001f))
            throw new InvalidOperationException("Sprite Prism changed its entity collider.");
        Vector2 point = Surface.SceneToRoot(new Vector2(12, 12));
        if (!ReferenceEquals(new HitTestService().HitTest(Surface.Root!, point.X, point.Y)?.Element, entity))
            throw new InvalidOperationException("Sprite Prism changed the UI geometric picking target.");
        point = Surface.SceneToRoot(new Vector2(75, 51));
        if (!ReferenceEquals(new HitTestService().HitTest(Surface.Root!, point.X, point.Y)?.Element, tile))
            throw new InvalidOperationException("Promoted-tile Prism changed UI geometric picking.");
    }

    internal static void VerifyCaptures(string directory, string backend)
    {
        List<object> frames = [];
        Dictionary<string, SKColor[]> initial = [];
        Dictionary<string, SKColor[]> previous = [];
        for (int sample = 0; sample < CaptureNames.Length; sample++)
        {
            string path = Path.Combine(directory, CaptureNames[sample]);
            using SKBitmap bitmap = SKBitmap.Decode(path) ?? throw new InvalidOperationException($"Cannot decode {path}");
            // Public native harness: inspect Window-owned captures, not internal
            // command/counter APIs (those contracts have focused core tests).
            foreach ((string name, int x, int y, int step, bool loopsUnchanged) in new[]
            {
                ("Reference", 10, 50, 4, true), ("SpritePrism", 10, 10, 4, false),
                ("GroupPrism", 39, 11, 4, true), ("TilePrism", 73, 50, 1, false)
            })
            {
                SKColor[] pixels = (from row in Enumerable.Range(0, 4)
                    from column in Enumerable.Range(0, 4)
                    select bitmap.GetPixel((x + column * step) * bitmap.Width / 128,
                        (y + row * step) * bitmap.Height / 84)).ToArray();
                if (sample == 0) initial[name] = pixels;
                if (sample > 0 && pixels.SequenceEqual(previous[name]))
                    throw new InvalidOperationException($"{backend}: {name} pixels did not change at sample {sample}.");
                if (sample == 3 && loopsUnchanged && !pixels.SequenceEqual(initial[name]))
                    throw new InvalidOperationException($"{backend}: loop endpoint did not restore {name} pixels.");
                previous[name] = pixels;
            }
            frames.Add(new { TimeMilliseconds = sample * 100, File = CaptureNames[sample], bitmap.Width, bitmap.Height,
                Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        }
        File.WriteAllText(Path.Combine(directory, "animation-backend.json"), JsonSerializer.Serialize(new
        {
            Backend = backend, CaptureApi = "Window.SaveScreenshot", Frames = frames,
            GeometryPicking = "PASS", AnimatedSpriteGroupTilePixels = "PASS", LoopPixels = "PASS",
            Timing = "Exact 100 ms UI-surface pulses; playback paused between captures; Motion uses explicit completion endpoints."
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose() { foreach (IDisposable effect in effects) effect.Dispose(); }

    private static void WriteAtlas(string path)
    {
        using SKBitmap bitmap = new(48, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 48; x++)
                bitmap.SetPixel(x, y, new SKColor((byte)(40 + x / 16 * 70), (byte)(35 + x % 16 * 9), (byte)(30 + y * 11)));
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
    }
}

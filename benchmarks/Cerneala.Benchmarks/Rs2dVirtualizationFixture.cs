using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

// Shared fixture of the RenderSurface2D virtualization benchmarks (--rs2d-ordering, --rs2d-groups, --rs2d-sdlgpu).
// An 800x600 camera pans over a world of N 16 px sprites placed on a 96 px grid, so the visible set stays constant
// (about 70 sprites) while N grows. The grid cells are assigned to children through a fixed-seed shuffle, so child
// order never correlates with position.
internal enum Rs2dStressKind { SceneSprites, Items, GroupWithSprite, EmptyGroup, TileMaps, ItemsOne }

internal enum Rs2dStressCamera { Pan, Static }

internal sealed record Rs2dStressSpec(
    string Name,
    Rs2dStressKind Kind,
    SceneOrderMode Order = SceneOrderMode.Source,
    bool Animated = false,
    Rs2dStressCamera Camera = Rs2dStressCamera.Pan,
    RenderSurface2DRedrawMode Redraw = RenderSurface2DRedrawMode.Continuous,
    bool RandomLayers = false,
    int[]? DefaultSizes = null,
    bool Gated = false);

internal sealed record Rs2dStressCell(float X, float Y);

internal sealed class Rs2dStressScene
{
    internal const float ViewWidth = 800;
    internal const float ViewHeight = 600;
    internal const float Spacing = 96;
    internal const float SpriteSize = 16;
    internal const int PanRange = 300;

    internal Rs2dStressScene(Scene2D scene, SceneItems2D? items, List<SceneItems2D> singleItems,
        float cameraX, float cameraY, double buildMilliseconds)
    {
        Scene = scene;
        Items = items;
        SingleItems = singleItems;
        CameraX = cameraX;
        CameraY = cameraY;
        BuildMilliseconds = buildMilliseconds;
    }

    internal Scene2D Scene { get; }

    // The one SceneItems2D that holds all N cells (Rs2dStressKind.Items).
    internal SceneItems2D? Items { get; }

    // The N SceneItems2D that hold one cell each (Rs2dStressKind.ItemsOne).
    internal List<SceneItems2D> SingleItems { get; }

    internal float CameraX { get; }

    internal float CameraY { get; }

    internal double BuildMilliseconds { get; }

    internal static Rs2dStressScene Create(Rs2dStressSpec spec, int n, ImageReference image)
    {
        int cols = (int)Math.Ceiling(Math.Sqrt(n));
        float world = cols * Spacing;
        float cameraX = world / 2 - ViewWidth / 2 - 150, cameraY = world / 2 - ViewHeight / 2;
        int[] order = new int[n];
        for (int i = 0; i < n; i++) { order[i] = i; }
        Random random = new(12345);
        for (int i = n - 1; i > 0; i--) { int j = random.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
        SpriteAnimationSet? clips = spec.Animated ? Clips() : null;
        (float x, float y) Pos(int k) => (order[k] % cols * Spacing, order[k] / cols * Spacing);
        Sprite2D MakeSprite(float x, float y) => clips is null
            ? new Sprite2D { Image = image, X = x, Y = y, Width = SpriteSize, Height = SpriteSize }
            : new Sprite2D { Image = image, X = x, Y = y, Width = SpriteSize, Height = SpriteSize, Animations = clips, AnimationState = "Walk" };

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Scene2D scene = new() { OrderMode = spec.Order };
        SceneItems2D? items = null;
        List<SceneItems2D> singleItems = [];
        switch (spec.Kind)
        {
            case Rs2dStressKind.SceneSprites:
                for (int k = 0; k < n; k++)
                {
                    (float x, float y) = Pos(k);
                    Sprite2D sprite = MakeSprite(x, y);
                    if (spec.RandomLayers) { sprite.Layer = random.Next(4); }
                    scene.Children.Add(sprite);
                }
                break;
            case Rs2dStressKind.GroupWithSprite:
            case Rs2dStressKind.EmptyGroup:
                for (int k = 0; k < n; k++)
                {
                    (float x, float y) = Pos(k);
                    Scene2D group = new() { X = x, Y = y };
                    if (spec.Kind == Rs2dStressKind.GroupWithSprite) { group.Children.Add(MakeSprite(0, 0)); }
                    scene.Children.Add(group);
                }
                break;
            case Rs2dStressKind.TileMaps:
            {
                TileSet2D tileSet = new("Terrain", new ResourceId<ImageResource>("Atlas"),
                    [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]);
                for (int k = 0; k < n; k++)
                {
                    (float x, float y) = Pos(k);
                    TileCell2D[] cells = new TileCell2D[16];
                    for (int i = 0; i < 16; i++) { cells[i] = new TileCell2D(1); }
                    TileMap2D map = TileMap2D.FromModel(new TileMap2DModel("Ground", new DrawSize(16, 16), [tileSet],
                        [new TileChunk2D(default, 4, 4, cells)], new TileMapBounds2D(0, 0, 4, 4)));
                    map.TranslateX = x;
                    map.TranslateY = y;
                    scene.Children.Add(map);
                }
                break;
            }
            case Rs2dStressKind.ItemsOne:
            {
                ContentTemplate<Rs2dStressCell> template = new("cell", null, 0, context => MakeSprite(context.Data!.X, context.Data.Y));
                for (int k = 0; k < n; k++)
                {
                    (float x, float y) = Pos(k);
                    SceneItems2D one = new();
                    one.Templates.Add(template);
                    one.ItemsSource = new[] { new Rs2dStressCell(x, y) };
                    scene.Children.Add(one);
                    singleItems.Add(one);
                }
                break;
            }
            case Rs2dStressKind.Items:
                Rs2dStressCell[] all = new Rs2dStressCell[n];
                for (int k = 0; k < n; k++) { (float x, float y) = Pos(k); all[k] = new Rs2dStressCell(x, y); }
                items = new SceneItems2D();
                items.Templates.Add(new ContentTemplate<Rs2dStressCell>("cell", null, 0, context => MakeSprite(context.Data!.X, context.Data.Y)));
                items.ItemsSource = all;
                scene.Children.Add(items);
                break;
        }
        return new Rs2dStressScene(scene, items, singleItems, cameraX, cameraY, stopwatch.Elapsed.TotalMilliseconds);
    }

    // The camera of frame number frameIndex; a static camera keeps the initial position.
    internal DrawRect ViewBoxAt(Rs2dStressCamera camera, int frameIndex) =>
        new(camera == Rs2dStressCamera.Pan ? CameraX + PingPong(frameIndex, PanRange) : CameraX, CameraY, ViewWidth, ViewHeight);

    // Sprites whose 16 px rectangle intersects the view, counted from the grid arithmetic alone (not from the scene).
    // Min excludes rectangles that only touch the view edge, Max includes them.
    internal static (int Min, int Max) VisibleSprites(int n, DrawRect view)
    {
        int cols = (int)Math.Ceiling(Math.Sqrt(n));
        int min = 0, max = 0;
        int rows = (n + cols - 1) / cols;
        for (int row = 0; row < rows; row++)
        {
            float y = row * Spacing;
            bool rowOpen = y + SpriteSize > view.Y && y < view.Y + view.Height;
            bool rowClosed = y + SpriteSize >= view.Y && y <= view.Y + view.Height;
            if (!rowClosed) { continue; }
            for (int col = 0; col < cols && row * cols + col < n; col++)
            {
                float x = col * Spacing;
                if (x + SpriteSize >= view.X && x <= view.X + view.Width)
                {
                    max++;
                    if (rowOpen && x + SpriteSize > view.X && x < view.X + view.Width) { min++; }
                }
            }
        }
        return (min, max);
    }

    internal static float PingPong(int frame, int range)
    {
        int period = range * 2;
        int p = frame % period;
        return p <= range ? p : period - p;
    }

    // Image commands plus the sprites inside tile sprite batches.
    internal static int CountSprites(DrawCommandList commands)
    {
        int count = 0;
        for (int i = 0; i < commands.Count; i++)
        {
            DrawCommand command = commands[i];
            if (command.Kind == DrawCommandKind.DrawImage) { count++; }
            else if (command.Kind == DrawCommandKind.DrawSpriteBatch) { count += command.SpriteBatch!.Sprites.Count(); }
        }
        return count;
    }

    internal static int CountImages(DrawCommandList commands)
    {
        int count = 0;
        for (int i = 0; i < commands.Count; i++) { if (commands[i].Kind == DrawCommandKind.DrawImage) { count++; } }
        return count;
    }

    internal static double Percentile(long[] values, double percentile)
    {
        long[] sorted = (long[])values.Clone();
        Array.Sort(sorted);
        int index = Math.Min(sorted.Length - 1, (int)Math.Ceiling(percentile * sorted.Length) - 1);
        return sorted[Math.Max(0, index)];
    }

    internal static double Percentile(double[] values, double percentile)
    {
        double[] sorted = (double[])values.Clone();
        Array.Sort(sorted);
        int index = Math.Min(sorted.Length - 1, (int)Math.Ceiling(percentile * sorted.Length) - 1);
        return sorted[Math.Max(0, index)];
    }

    // Sizes from the optional trailing command-line argument ("1000,10000"), else null.
    internal static int[]? ParseSizes(string[] arguments) =>
        arguments.Length == 0
            ? null
            : arguments[0].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(size => int.Parse(size, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    private static SpriteAnimationSet Clips() => new([
        new SpriteAnimationClip("Walk", [
            new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(32, 0, 16, 16), TimeSpan.FromMilliseconds(100))], true)]);
}

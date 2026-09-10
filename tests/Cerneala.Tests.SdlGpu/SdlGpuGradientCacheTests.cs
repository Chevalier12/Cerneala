using System.Collections;
using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuGradientCacheTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangingGradientReusesReleasedNativeStorage(bool radial)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(api, factory, 1);
        for (int frame = 0; frame < 2; frame++) DrawChangedGradient();
        int createdAfterWarmup = api.TextureCreationCount;
        for (int frame = 0; frame < 96; frame++) DrawChangedGradient();

        Assert.Equal(createdAfterWarmup, api.TextureCreationCount);
        Assert.All(api.GpuTextureUploads, upload => Assert.True(upload.Cycle));
        Assert.Equal(2, api.GpuTextures.Values.Count(
            texture => texture.CreateInfo.Usage == SdlGpuTextureUsage.Sampler));

        Render(session, new DrawCommandList(), new CountingGradient(radial));
        Assert.Equal(0, session.DrawingResources.CachedTextureCount);
        // EndFrame retires brush storage after the session's retirement flush;
        // the following completion flushes it, as with other retired textures.
        Render(session, new DrawCommandList(), new CountingGradient(radial));
        Assert.DoesNotContain(api.GpuTextures.Values,
            texture => texture.CreateInfo.Usage == SdlGpuTextureUsage.Sampler);

        void DrawChangedGradient()
        {
            // Distinct brush identity deliberately misses the content cache.
            CountingGradient brush = new(radial);
            Render(session, Commands(brush, new DrawRect(0, 0, 32, 24)), brush);
            Assert.True(brush.Stops.ReadCount > 0);
        }
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecycledGradientStorageKeepsEveryNewFramePixelExact(bool radial)
    {
        DrawCommandList[] scenes = new DrawCommandList[24];
        Color[][] expected = new Color[scenes.Length][];
        DrawRect bounds = new(3, 4, 32, 24);
        for (int frame = 0; frame < scenes.Length; frame++)
        {
            Color color = new((byte)(frame * 9), (byte)(255 - frame * 7), (byte)(frame * 5), (byte)(128 + frame * 4));
            IDrawBrush brush = radial
                ? new RadialGradientBrush(new DrawPoint(16, 12), 16, 12,
                    [new(0, Color.White), new(1, color)], .63f)
                : new LinearGradientBrush(default, new DrawPoint(32, 0),
                    [new(0, Color.White), new(1, color)], .63f);
            scenes[frame] = Commands(brush, bounds);
        }
        // A fixture owns SDL's process-wide platform lifetime. Do not overlap
        // independent fixtures: disposing either one calls SDL_Quit.
        using (SdlDrawingFixture reference = new(64, 48))
        {
            for (int frame = 0; frame < scenes.Length; frame++)
            {
                reference.Render(new DrawCommandList(), Color.Transparent);
                expected[frame] = reference.Render(scenes[frame], Color.Transparent);
            }
        }
        using SdlDrawingFixture reused = new(64, 48);
        for (int frame = 0; frame < scenes.Length; frame++)
        {
            Assert.Equal(expected[frame], reused.Render(scenes[frame], Color.Transparent));
        }
    }

    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 1f)]
    [InlineData(false, 1.25f)]
    [InlineData(true, 1.25f)]
    public void AxisAlignedGradientStoresOnlyDistinctTexels(bool vertical, float scale)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(api, factory, scale);
        CountingGradient brush = new(false, vertical ? new DrawPoint(0, 24) : new DrawPoint(32, 0));

        Render(session, Commands(brush, new DrawRect(0, 0, 32, 24)), brush);

        var texture = Assert.Single(api.GpuTextures.Values,
            item => item.CreateInfo.Usage == SdlGpuTextureUsage.Sampler);
        Assert.Equal(vertical ? 1u : (uint)MathF.Ceiling(32 * scale), texture.CreateInfo.Width);
        Assert.Equal(vertical ? (uint)MathF.Ceiling(24 * scale) : 1u, texture.CreateInfo.Height);
    }

    [SdlNativeTheory]
    [InlineData(false, 1f)]
    [InlineData(true, 1f)]
    [InlineData(false, 1.25f)]
    [InlineData(true, 1.25f)]
    [InlineData(false, 1.5f)]
    [InlineData(true, 1.5f)]
    [InlineData(false, 2f)]
    [InlineData(true, 2f)]
    public void AxisAlignedTextureMatchesFullTwoDimensionalRaster(bool vertical, float scale)
    {
        using SdlDrawingFixture fixture = new(128, 96, coordinateScale: scale);
        var rasterize = typeof(SdlGpuDrawingBackend)
            .GetMethod("RasterizeBrush", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<DrawBrushDescriptor, DrawRect, int, int, byte[]>>();
        foreach (DrawRect bounds in new[] { new DrawRect(7, 9, 32, 24), new DrawRect(7.2f, 9.3f, 17.3f, 13.7f), new DrawRect(7, 9, 1, 1) })
        foreach (bool reverse in new[] { false, true })
        foreach (float opacity in new[] { 1f, .63f })
        {
            DrawPoint start = new(3, 5);
            DrawPoint end = vertical ? new(3, 19) : new(27, 5);
            if (reverse) (start, end) = (end, start);
            LinearGradientBrush brush = new(start, end,
                [new(0, Color.Red), new(.3f, new Color(17, 201, 43, 137)),
                 new(.3f, new Color(211, 33, 178, 201)), new(1, Color.Transparent)], opacity);
            int width = (int)MathF.Ceiling(bounds.Width * scale);
            int height = (int)MathF.Ceiling(bounds.Height * scale);
            using SdlGpuImage reference = new(width, height,
                rasterize(((IDrawBrush)brush).CreateDescriptor(), bounds, width, height));
            Color[] expected = fixture.Render(Scene(new ImageBrush(reference)), Color.Transparent);
            Color[] actual = fixture.Render(Scene(brush), Color.Transparent);
            Assert.Equal(expected, actual);

            DrawCommandList scene = Scene(brush);
            Assert.Equal(actual, fixture.Render(scene, Color.Transparent));

            DrawCommandList Scene(IDrawBrush paint)
            {
                DrawCommandList commands = new();
                commands.Add(DrawCommand.PushTransform(System.Numerics.Matrix3x2.CreateRotation(.13f) *
                    System.Numerics.Matrix3x2.CreateTranslation(8, 2)));
                commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 50, 40)));
                commands.Add(DrawCommand.FillRectangle(bounds, paint, .72f));
                commands.Add(DrawCommand.PopClip());
                commands.Add(DrawCommand.PopTransform());
                return commands;
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstAxisAlignedRasterSamplesOnlyTheVaryingCoordinate(bool vertical)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(api, factory, 1);
        CountingGradient brush = new(false, vertical ? new DrawPoint(0, 24) : new DrawPoint(32, 0));

        Render(session, Commands(brush, new DrawRect(0, 0, 32, 24)), brush);

        // Three stop reads per scalar sample for this two-stop brush. Identical
        // rows/columns must not repeat gradient evaluation on the first cache miss.
        Assert.InRange(brush.Stops.ReadCount, 1, (vertical ? 24 : 32) * 3);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AxisAlignedCompactRasterIsByteExactWithPerPixelSampling(bool vertical, bool reverse)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var rasterize = typeof(SdlGpuDrawingBackend).GetMethod("RasterizeBrush", flags)!
            .CreateDelegate<Func<DrawBrushDescriptor, DrawRect, int, int, byte[]>>();
        var sample = typeof(SdlGpuDrawingBackend).GetMethod("SampleBrush", flags)!
            .CreateDelegate<Func<DrawBrushDescriptor, DrawPoint, Color>>();
        var write = typeof(SdlGpuDrawingBackend).GetMethod("WritePremultipliedColor", flags)!
            .CreateDelegate<Action<byte[], int, Color>>();
        DrawGradientStop[] stops =
        [
            new(0, new Color(213, 17, 45, 0)),
            new(.3f, new Color(41, 176, 205, 137)),
            new(.3f, new Color(217, 57, 114, 201)),
            new(1, new Color(13, 81, 255, 255))
        ];
        foreach (float scale in new[] { 1f, 1.25f, 1.5f, 2f })
        foreach (float opacity in new[] { 0f, .63f, 1f })
        foreach (int size in new[] { 1, 17, 220 })
        {
            DrawRect bounds = new(7, 11, size, size == 1 ? 1 : 23);
            DrawPoint start = new(3, 5);
            DrawPoint end = vertical ? new(3, 19) : new(15, 5);
            if (reverse) (start, end) = (end, start);
            Verify(new LinearGradientDrawBrushDescriptor(start, end, stops, opacity), bounds, scale);
            Verify(new LinearGradientDrawBrushDescriptor(start, start, stops, opacity), bounds, scale);
        }

        // Brush endpoints are finite but can exceed the pixel-coordinate range.
        // Exercise those endpoints with the largest supported logical bounds.
        DrawPoint extremeStart = vertical ? new(-3e38f, 0) : new(0, -3e38f);
        DrawPoint extremeEnd = vertical ? new(-3e38f, 10) : new(10, -3e38f);
        Verify(new LinearGradientDrawBrushDescriptor(extremeStart, extremeEnd, stops, .63f),
            new DrawRect(0, 0, vertical ? 2_000_000_000f : 20, vertical ? 20 : 2_000_000_000f), 1, 17, 13);

        void Verify(LinearGradientDrawBrushDescriptor descriptor, DrawRect bounds, float scale,
            int? pixelWidth = null, int? pixelHeight = null)
        {
            int width = pixelWidth ?? (int)MathF.Ceiling(bounds.Width * scale);
            int height = pixelHeight ?? (int)MathF.Ceiling(bounds.Height * scale);
            byte[] expected = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                DrawPoint point = new(((x + .5f) / width) * bounds.Width,
                    ((y + .5f) / height) * bounds.Height);
                write(expected, ((y * width) + x) * 4, sample(descriptor, point));
            }
            int compactWidth = vertical ? 1 : width;
            int compactHeight = vertical ? height : 1;
            byte[] compact = rasterize(descriptor, bounds, compactWidth, compactHeight);
            byte[] expanded = new byte[expected.Length];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int source = ((vertical ? y : 0) * compactWidth + (vertical ? 0 : x)) * 4;
                compact.AsSpan(source, 4).CopyTo(expanded.AsSpan(((y * width) + x) * 4, 4));
            }
            Assert.Equal(expected, expanded);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachedGradientDoesNotSamplePixelsAgain(bool radial)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(api, factory, 1);
        CountingGradient brush = new(radial);
        DrawCommandList commands = Commands(brush, new DrawRect(0, 0, 32, 24));

        Render(session, commands, brush);
        Assert.True(brush.Stops.ReadCount > 0);
        for (int frame = 0; frame < 16; frame++)
        {
            Render(session, commands, brush);
            Assert.Equal(0, brush.Stops.ReadCount);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedGradientReusesPixelsButChangedBoundsScaleAndRetirementRecreateThem(bool radial)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession first = CreateSession(api, factory, 1);
        using SdlGpuWindowGraphicsSession second = CreateSession(api, factory, 1);
        using SdlGpuWindowGraphicsSession scaled = CreateSession(api, factory, 1.5f);
        CountingGradient brush = new(radial);
        DrawCommandList commands = Commands(brush, new DrawRect(0, 0, 32, 24));
        Render(first, commands, brush);
        Assert.True(brush.Stops.ReadCount > 0);
        Render(second, commands, brush);
        Assert.Equal(0, brush.Stops.ReadCount);

        Render(first, new DrawCommandList(), brush);
        Render(second, commands, brush);
        Assert.Equal(0, brush.Stops.ReadCount);
        Render(scaled, commands, brush);
        Assert.True(brush.Stops.ReadCount > 0);
        Render(scaled, commands, brush);
        Assert.Equal(0, brush.Stops.ReadCount);

        DrawCommandList resized = Commands(brush, new DrawRect(0, 0, 40, 28));
        Render(first, resized, brush);
        Assert.True(brush.Stops.ReadCount > 0);
        Render(first, resized, brush);
        Assert.Equal(0, brush.Stops.ReadCount);

        Render(first, new DrawCommandList(), brush);
        Render(second, new DrawCommandList(), brush);
        Render(scaled, new DrawCommandList(), brush);
        Render(first, commands, brush);
        Assert.True(brush.Stops.ReadCount > 0);
    }

    [SdlNativeTheory]
    [InlineData(false, 1f)]
    [InlineData(false, 1.25f)]
    [InlineData(false, 1.5f)]
    [InlineData(false, 2f)]
    [InlineData(true, 1f)]
    [InlineData(true, 1.25f)]
    [InlineData(true, 1.5f)]
    [InlineData(true, 2f)]
    public void GradientReusePreservesPixelsAndDoesNotHideChangedPaint(bool radial, float scale)
    {
        using SdlDrawingFixture fixture = new(96, 64, coordinateScale: scale);
        DrawRect bounds = new(8, 8, 32, 24);
        DrawCommandList commands = Commands(CreateBrush(Color.Red), bounds);
        Color[] cold = fixture.Render(commands, Color.Transparent);
        Assert.Contains(cold, pixel => pixel.A > 0);
        for (int frame = 0; frame < 4; frame++)
        {
            // A newly constructed, value-equivalent immutable UI brush must reuse
            // the same valid paint without changing output.
            Assert.Equal(cold, fixture.Render(Commands(CreateBrush(Color.Red), bounds), Color.Transparent));
        }

        DrawCommandList changed = Commands(CreateBrush(Color.Blue), bounds);
        Color[] changedPixels = fixture.Render(changed, Color.Transparent);
        Assert.False(cold.SequenceEqual(changedPixels));
        Assert.Equal(changedPixels, fixture.Render(changed, Color.Transparent));
        DrawCommandList resized = Commands(CreateBrush(Color.Blue), new DrawRect(8, 8, 40, 28));
        Color[] resizedPixels = fixture.Render(resized, Color.Transparent);
        Assert.False(changedPixels.SequenceEqual(resizedPixels));
        Assert.Equal(resizedPixels, fixture.Render(resized, Color.Transparent));
        fixture.Render(new DrawCommandList(), Color.Transparent);
        Assert.Equal(cold, fixture.Render(commands, Color.Transparent));

        IDrawBrush CreateBrush(Color color) => radial
            ? new RadialGradientBrush(new DrawPoint(16, 12), 16, 12,
                [new GradientStop(0, Color.White), new GradientStop(1, color)], 0.6f)
            : new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(32, 24),
                [new GradientStop(0, Color.White), new GradientStop(1, color)], 0.6f);
    }

    private static SdlGpuWindowGraphicsSession CreateSession(
        FakeSdlApi api, SdlGpuWindowGraphicsSessionFactory factory, float scale)
    {
        nint window = api.CreateWindow("gradient-cache", 64, 48, SdlWindowOptions.Hidden);
        return Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)), 64, 48, scale));
    }

    private static DrawCommandList Commands(IDrawBrush brush, DrawRect bounds)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(bounds, brush));
        return commands;
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session, DrawCommandList commands, CountingGradient brush)
    {
        DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
        brush.Stops.ReadCount = 0;
        session.BeginFrame(Color.Transparent);
        try { session.DrawingBackend.Render(commands, in context); }
        finally { session.CompleteFrame(present: false); }
    }

    private sealed class CountingGradient(bool radial, DrawPoint? endPoint = null) : IDrawBrush
    {
        public CountingStops Stops { get; } = new();
        public DrawBrushKind Kind => radial ? DrawBrushKind.RadialGradient : DrawBrushKind.LinearGradient;
        public float Opacity => 1;
        public Color? SolidColor => null;
        public DrawBrushDescriptor CreateDescriptor() => radial
            ? new RadialGradientDrawBrushDescriptor(new DrawPoint(16, 12), 16, 12, Stops, 1)
            : new LinearGradientDrawBrushDescriptor(default, endPoint ?? new DrawPoint(32, 24), Stops, 1);
    }

    private sealed class CountingStops : IReadOnlyList<DrawGradientStop>
    {
        private readonly DrawGradientStop[] stops = [new(0, Color.Black), new(1, Color.White)];
        public int ReadCount { get; set; }
        public int Count => stops.Length;
        public DrawGradientStop this[int index]
        {
            get { ReadCount++; return stops[index]; }
        }
        public IEnumerator<DrawGradientStop> GetEnumerator() => ((IEnumerable<DrawGradientStop>)stops).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

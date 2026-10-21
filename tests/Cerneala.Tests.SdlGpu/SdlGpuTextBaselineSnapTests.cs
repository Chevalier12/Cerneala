using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

// Issue #719: on axis-aligned transforms the DEVICE baseline must be snapped
// (floor(v + 0.5) on (y * M22 + M32) * scale), so translating text by a
// fraction of a device pixel cannot move its ink by a whole row, and the
// vertical sub-pixel phase never takes part in the raster cache key.
public sealed class SdlGpuTextBaselineSnapTests
{
    private const int FrameWidth = 128;
    private const int FrameHeight = 64;
    private const string Sample = "Txe";

    [Collection(SdlNativeTestCollection.Name)]
    public sealed class Native
    {
        [SdlNativeFact]
        public void FractionalTranslateKeepsTheSnappedBaselineAndWholeDeviceRowsMoveInk()
        {
            const float scale = 1.25f;
            using SdlDrawingFixture fixture = new(FrameWidth, FrameHeight, coordinateScale: scale);
            IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 14);
            // Device baseline (24 * 1.25) = 30, device x = 8 * 1.25 = 10.
            DrawPoint baseline = new(8, 24);

            Color[] reference = Render(fixture, font, baseline, Matrix3x2.CreateTranslation(0, 0));
            Color[] fractional = Render(fixture, font, baseline, Matrix3x2.CreateTranslation(0, 0.2f));
            Color[] whole = Render(fixture, font, baseline, Matrix3x2.CreateTranslation(0, 1.0f));

            // 30.25 rounds to 30: same pixels as translate 0.
            AssertShifted(reference, fractional, 0, "translate 0.2 (device baseline 30.25) vs translate 0");
            // 31.25 rounds to 31: exactly one row lower.
            AssertShifted(reference, whole, 1, "translate 1.0 (device baseline 31.25) vs translate 0 moved down 1 row");
        }

        [SdlNativeFact]
        public void BaselineSnapsToNearestDeviceRowAtScale175()
        {
            const float scale = 1.75f;
            using SdlDrawingFixture fixture = new(FrameWidth, FrameHeight, coordinateScale: scale);
            IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 14);
            const float x = 8; // device x = 14

            // Device baseline 30 exactly (within float noise, far from a rounding boundary).
            Color[] row30 = Render(fixture, font, new DrawPoint(x, 30f / scale), null);
            // Device baseline 30.438 rounds DOWN to 30: ink on row 30, not 31.
            Color[] below = Render(fixture, font, new DrawPoint(x, 30.438f / scale), null);
            AssertShifted(row30, below, 0, "device baseline 30.438 vs device baseline 30");

        }

        [SdlNativeFact]
        public void HalfDeviceRowBaselineRoundsUpAtScale175()
        {
            const float scale = 1.75f;
            using SdlDrawingFixture fixture = new(FrameWidth, FrameHeight, coordinateScale: scale);
            IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 14);
            const float x = 8; // device x = 14

            // Device baseline 31 and the exactly representable half-pixel 31.5 (y = 18, 18 * 1.75).
            Color[] row31 = Render(fixture, font, new DrawPoint(x, 31f / scale), null);
            Color[] half = Render(fixture, font, new DrawPoint(x, 18f), null);
            // Half rounds up (as today): 31.5 -> 32 = the row-31 pixels moved down 1 row.
            AssertShifted(row31, half, 1, "device baseline 31.5 vs device baseline 31 moved down 1 row");
        }

        [SdlNativeTheory]
        [InlineData("rotate")]
        [InlineData("skew")]
        public void RotatedAndSkewedTextKeepsTheLocalRowRule(string kind)
        {
            using SdlDrawingFixture fixture = new(FrameWidth, FrameHeight, coordinateScale: 1f);
            IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 14);
            Matrix3x2 matrix = kind == "rotate"
                ? Matrix3x2.CreateRotation(MathF.PI / 6) * Matrix3x2.CreateTranslation(40, 5)
                : Matrix3x2.CreateSkew(0.3f, 0) * Matrix3x2.CreateTranslation(20, 5);

            Color[] f000 = Render(fixture, font, new DrawPoint(10, 20.0f), matrix);
            Color[] f040 = Render(fixture, font, new DrawPoint(10, 20.40f), matrix);
            Color[] f045 = Render(fixture, font, new DrawPoint(10, 20.45f), matrix);
            Color[] f050 = Render(fixture, font, new DrawPoint(10, 20.5f), matrix);

            // Row rule in local space: a scaled baseline fraction below 0.4375 keeps
            // its row, a fraction of 0.4375 or more lands one row lower.
            AssertShifted(f000, f040, 0, $"{kind}: local fraction 0.40 vs 0.0");
            AssertShifted(f045, f050, 0, $"{kind}: local fraction 0.50 vs 0.45");
            Assert.True(MaxChannelDifference(f000, f045) > 16,
                $"{kind}: fractions >= 0.4375 are expected to draw one row lower than fractions < 0.4375.");
        }
    }

    [Fact]
    public void SameRunAtDifferentVerticalFractionsSharesOneAtlasEntrySet()
    {
        using FakeFixture fixture = new(1.25f);
        // Device baselines 10.0 and 10.5 (local y 8.0 and 8.4), same device x = 5.
        fixture.Render(new DrawPoint(4, 8.0f), null);
        int afterFirst = fixture.Resources.TextAtlasEntryCount;
        Assert.Equal(3, afterFirst);

        fixture.Render(new DrawPoint(4, 8.4f), null);

        Assert.True(fixture.Resources.TextAtlasEntryCount == afterFirst,
            $"Atlas entries after the second baseline: {fixture.Resources.TextAtlasEntryCount}; " +
            $"expected {afterFirst} (the vertical fraction must not be part of the raster key).");
        Assert.True(fixture.Backend.LastFrameTiming.TextRequestCount == 0,
            $"Second frame rasterized {fixture.Backend.LastFrameTiming.TextRequestCount} text run(s); expected 0.");
    }

    [Fact]
    public void FractionalTranslateDoesNotMoveTheTextQuadAndWholeRowTranslateMovesItOneDeviceRow()
    {
        const float scale = 1.25f;
        // Local baseline y = 24 -> device 30.
        float noTransform = TextQuadTopDeviceY(scale, null);
        float fractional = TextQuadTopDeviceY(scale, Matrix3x2.CreateTranslation(0, 0.2f));
        float whole = TextQuadTopDeviceY(scale, Matrix3x2.CreateTranslation(0, 1.0f));

        // 30.25 rounds to 30.
        Assert.True(MathF.Abs(fractional - noTransform) < 1e-3f,
            $"Text quad top with translate 0.2 is {fractional}; expected {noTransform} (no transform).");
        // 31.25 rounds to 31 (1.25 device pixels of translation draw one device row lower).
        Assert.True(MathF.Abs(whole - (noTransform + 1)) < 1e-3f,
            $"Text quad top with translate 1.0 is {whole}; expected {noTransform + 1}.");
    }

    private static float TextQuadTopDeviceY(float scale, Matrix3x2? transform)
    {
        using FakeFixture fixture = new(scale, captureVertices: true);
        fixture.Render(new DrawPoint(4, 24), transform);
        SdlGpuVertex[] vertices = fixture.CapturedVertices();
        Assert.Equal(12, vertices.Length); // three coverage channels, four vertices each
        return vertices.Min(vertex => vertex.Position.Y);
    }

    private static Color[] Render(SdlDrawingFixture fixture, IDrawFont font, DrawPoint baseline, Matrix3x2? transform)
        => fixture.Render(CreateCommands(font, baseline, transform), Color.Black);

    private static DrawCommandList CreateCommands(IDrawFont font, DrawPoint baseline, Matrix3x2? transform)
    {
        DrawCommandList commands = new();
        if (transform is { } matrix)
        {
            commands.Add(DrawCommand.PushTransform(matrix));
        }
        commands.Add(DrawCommand.DrawText(new DrawTextRun(font, Sample, 14), baseline, Color.White));
        if (transform is not null)
        {
            commands.Add(DrawCommand.PopTransform());
        }
        return commands;
    }

    private static bool IsInk(Color pixel) => pixel.R != 0 || pixel.G != 0 || pixel.B != 0;

    private static int LowestInkRow(Color[] pixels)
    {
        for (int index = pixels.Length - 1; index >= 0; index--)
        {
            if (IsInk(pixels[index]))
            {
                return index / FrameWidth;
            }
        }
        return -1;
    }

    private static int MaxChannelDifference(Color[] first, Color[] second)
    {
        int max = 0;
        for (int index = 0; index < first.Length; index++)
        {
            max = Math.Max(max, Math.Abs(first[index].R - second[index].R));
            max = Math.Max(max, Math.Abs(first[index].G - second[index].G));
            max = Math.Max(max, Math.Abs(first[index].B - second[index].B));
        }
        return max;
    }

    // Asserts actual == reference moved down by rowShift rows (rows entering from the top are clear).
    private static void AssertShifted(Color[] reference, Color[] actual, int rowShift, string label)
    {
        Assert.Equal(FrameWidth * FrameHeight, reference.Length);
        Assert.Equal(reference.Length, actual.Length);
        int referenceLowest = LowestInkRow(reference);
        Assert.True(referenceLowest >= 0, $"{label}: the reference frame has no ink.");
        Assert.True(referenceLowest + rowShift + 2 < FrameHeight,
            $"{label}: ink reaches row {referenceLowest}, too close to the frame bottom for a {rowShift}-row shift.");

        for (int y = 0; y < FrameHeight; y++)
        {
            for (int x = 0; x < FrameWidth; x++)
            {
                int sourceY = y - rowShift;
                Color expected = sourceY >= 0 ? reference[(sourceY * FrameWidth) + x] : Color.Black;
                Color got = actual[(y * FrameWidth) + x];
                Assert.True(expected == got,
                    $"{label}: first differing pixel at (x={x}, y={y}): expected {expected}, actual {got}. " +
                    $"Lowest ink row: reference+shift={referenceLowest + rowShift}, actual={LowestInkRow(actual)}.");
            }
        }
    }

    private sealed class FakeFixture : IDisposable
    {
        private readonly FakeSdlApi api;
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        private readonly IDrawFont font = new SystemFontSource().LoadFont("Arial", 16);
        private readonly SdlGpuWindowGraphicsSession session;

        internal FakeFixture(float scale, bool captureVertices = false)
        {
            api = new FakeSdlApi { WindowPixelDensity = 1, CaptureGpuBufferUploads = captureVertices };
            nint window = api.CreateWindow("text baseline snap", 160, 80, SdlWindowOptions.Hidden);
            factory = new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling: false);
            session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)), 160, 80, scale));
        }

        internal SdlGpuDrawingResources Resources => session.DrawingResources;

        internal SdlGpuDrawingBackend Backend => Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);

        internal void Render(DrawPoint baseline, Matrix3x2? transform)
        {
            DrawCommandList commands = new();
            if (transform is { } matrix)
            {
                commands.Add(DrawCommand.PushTransform(matrix));
            }
            commands.Add(DrawCommand.DrawText(new DrawTextRun(font, Sample, 16), baseline, Color.White));
            if (transform is not null)
            {
                commands.Add(DrawCommand.PopTransform());
            }
            DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
            session.BeginFrame(Color.Transparent);
            try { session.DrawingBackend.Render(commands, in frame); }
            finally { session.CompleteFrame(present: false); }
        }

        internal SdlGpuVertex[] CapturedVertices()
        {
            List<SdlGpuVertex> vertices = [];
            foreach (var upload in api.GpuBufferUploads.Where(upload =>
                api.GpuBuffers[upload.Buffer].CreateInfo.Usage == SdlGpuBufferUsage.Vertex))
            {
                ReadOnlySpan<byte> bytes = api.GpuBuffers[upload.Buffer].Data.Span.Slice(
                    checked((int)upload.BufferOffset), checked((int)upload.Size));
                vertices.AddRange(MemoryMarshal.Cast<byte, SdlGpuVertex>(bytes).ToArray());
            }
            return vertices.ToArray();
        }

        public void Dispose()
        {
            session.Dispose();
            factory.Dispose();
        }
    }
}

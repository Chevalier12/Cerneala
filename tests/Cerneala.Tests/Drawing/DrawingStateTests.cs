using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingStateTests
{
    [Fact]
    public void AnalyzerAllocatesOneOwnedEntrySnapshotAndKeepsItImmutable()
    {
        const int commandCount = 1024;
        DrawCommand command = DrawCommand.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White);
        DrawCommandList commands = new();
        for (int index = 0; index < commandCount; index++)
        {
            commands.Add(command);
        }
        DrawCommandStateAnalyzer analyzer = new();
        for (int warmup = 0; warmup < 8; warmup++)
        {
            GC.KeepAlive(analyzer.Analyze(commands));
        }

        long metadataBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < commandCount; index++)
        {
            GC.KeepAlive(DrawCommandMetadata.Create(command));
        }
        long metadataBytes = GC.GetAllocatedBytesForCurrentThread() - metadataBefore;
        long before = GC.GetAllocatedBytesForCurrentThread();
        DrawCommandStateAnalysis first = analyzer.Analyze(commands);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        long budget = metadataBytes +
            commandCount * System.Runtime.CompilerServices.Unsafe.SizeOf<DrawCommandStateEntry>() + 8192;
        Assert.True(allocatedBytes <= budget,
            $"State analysis allocated {allocatedBytes:N0} bytes; one entry snapshot plus metadata allows {budget:N0}.");

        commands.Clear();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(20, 30, 40, 50), Color.Black));
        DrawCommandStateAnalysis second = analyzer.Analyze(commands);
        Assert.Equal(commandCount, first.Entries.Count);
        Assert.Equal(new DrawRect(1, 2, 3, 4), first.Entries[0].Bounds);
        Assert.Equal(new DrawRect(20, 30, 40, 50), second.Entries[0].Bounds);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<DrawCommandStateEntry>)first.Entries)[0] = default);
    }

    [Fact]
    public void AnalyzerComposesParentThenChildAndReportsWorldBounds()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(Matrix3x2.CreateTranslation(10, 20));
        drawing.PushTransform(Matrix3x2.CreateScale(2));
        drawing.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White);
        drawing.PopTransform();
        drawing.PopTransform();

        DrawCommandStateAnalysis analysis =
            new DrawCommandStateAnalyzer().Analyze(commands);

        Assert.Equal(
            Matrix3x2.Multiply(
                Matrix3x2.CreateScale(2),
                Matrix3x2.CreateTranslation(10, 20)),
            analysis.Entries[2].Transform);
        Assert.Equal(new DrawRect(12, 24, 6, 8), analysis.Entries[2].Bounds);
        Assert.Equal(4, analysis.Entries[0].MatchingCommandIndex);
        Assert.Equal(3, analysis.Entries[1].MatchingCommandIndex);
    }

    [Fact]
    public void AnalyzerRejectsMixedStackImbalanceWithPushIndex()
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushTransform(Matrix3x2.Identity));
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.PopTransform());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new DrawCommandStateAnalyzer().Analyze(commands));

        Assert.Contains("command index 2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("command index 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not LIFO", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefScopesRequireLifoAndRejectDoubleDispose()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawTransformScope transform = drawing.Transform(Matrix3x2.Identity);
        DrawOpacityScope opacity = drawing.Opacity(0.5f);

        bool outOfOrderRejected = false;
        try
        {
            transform.Dispose();
        }
        catch (InvalidOperationException)
        {
            outOfOrderRejected = true;
        }
        Assert.True(outOfOrderRejected);
        opacity.Dispose();
        transform.Dispose();
        bool doubleDisposeRejected = false;
        try
        {
            transform.Dispose();
        }
        catch (ObjectDisposedException)
        {
            doubleDisposeRejected = true;
        }
        Assert.True(doubleDisposeRejected);
        Assert.Equal(
            [
                DrawCommandKind.PushTransform,
                DrawCommandKind.PushOpacity,
                DrawCommandKind.PopOpacity,
                DrawCommandKind.PopTransform
            ],
            commands.Select(command => command.Kind));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void TransformGeometricClipAndGroupOpacityRenderAndRestoreState(float scale)
    {
        using SdlDrawingFixture fixture = new((int)(96 * scale), (int)(64 * scale), coordinateScale: scale);
        DrawPath clip = new DrawPathBuilder()
            .MoveTo(new DrawPoint(16, 8))
            .LineTo(new DrawPoint(48, 8))
            .LineTo(new DrawPoint(16, 40))
            .Close()
            .Build();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(Matrix3x2.CreateTranslation(8, 0));
        drawing.PushClip(clip, DrawFillRule.NonZero);
        drawing.PushOpacity(0.5f);
        drawing.FillRectangle(new DrawRect(8, 8, 24, 24), Color.White);
        drawing.FillRectangle(new DrawRect(16, 8, 24, 24), Color.White);
        drawing.PopOpacity();
        drawing.PopClip();
        drawing.PopTransform();

        Color[] pixels = fixture.Render(commands);
        // After translation, the triangle's diagonal is x + y = 64.
        // Sample logical coordinates so DPI cannot move a probe across it.
        Color single = fixture.Sample(pixels, 44, 16);
        Color overlap = fixture.Sample(pixels, 32, 16);
        Color clipped = fixture.Sample(pixels, 44, 28);

        Assert.InRange(single.R, 125, 130);
        Assert.InRange(overlap.R, 125, 130);
        Assert.Equal(Color.Black, clipped);

        DrawCommandList unscoped = new();
        new DrawingContext(unscoped).FillRectangle(new DrawRect(0, 0, 96, 64), Color.White);
        Assert.All(fixture.Render(unscoped), pixel => Assert.Equal(Color.White, pixel));
    }

    [Fact]
    public void AxisAlignedRectangleClipRestrictsPixels()
    {
        using SdlDrawingFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushClip(new DrawRect(12, 8, 24, 20));
        drawing.FillRectangle(new DrawRect(0, 0, 64, 48), Color.White);
        drawing.PopClip();

        Color[] pixels = fixture.Render(commands);

        Assert.Equal(Color.White, fixture.Sample(pixels, 20, 16));
        Assert.Equal(Color.Black, fixture.Sample(pixels, 8, 16));
    }

    [Fact]
    public void NestedGeometricClipsRenderTheirIntersection()
    {
        using SdlDrawingFixture fixture = new();
        DrawPath outer = RectanglePath(8, 8, 40, 32);
        DrawPath inner = RectanglePath(24, 0, 32, 32);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushClip(outer, DrawFillRule.NonZero);
        drawing.PushClip(inner, DrawFillRule.NonZero);
        drawing.FillRectangle(new DrawRect(0, 0, 64, 48), Color.White);
        drawing.PopClip();
        drawing.PopClip();

        Color[] pixels = fixture.Render(commands);

        Assert.Equal(Color.White, fixture.Sample(pixels, 30, 16));
        Assert.Equal(Color.Black, fixture.Sample(pixels, 16, 16));
        Assert.Equal(Color.Black, fixture.Sample(pixels, 52, 16));
    }

    [Fact]
    public void BasicBlendModesUsePremultipliedComposition()
    {
        using SdlDrawingFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.FillRectangle(new DrawRect(0, 0, 16, 16), new Color(0, 0, 200));
        drawing.PushBlend(DrawBlendMode.Opaque);
        drawing.FillRectangle(new DrawRect(0, 0, 16, 16), new Color(200, 0, 0, 128));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(20, 0, 16, 16), new Color(100, 0, 0));
        drawing.PushBlend(DrawBlendMode.Additive);
        drawing.FillRectangle(new DrawRect(20, 0, 16, 16), new Color(0, 80, 0));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(40, 0, 16, 16), new Color(200, 100, 50));
        drawing.PushBlend(DrawBlendMode.Multiply);
        drawing.FillRectangle(new DrawRect(40, 0, 16, 16), new Color(128, 128, 128));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(60, 0, 16, 16), new Color(100, 50, 0));
        drawing.PushBlend(DrawBlendMode.Screen);
        drawing.FillRectangle(new DrawRect(60, 0, 16, 16), new Color(128, 128, 128));
        drawing.PopBlend();

        Color[] pixels = fixture.Render(commands);

        AssertColorNear(new Color(100, 0, 0, 128), fixture.Sample(pixels, 8, 8));
        AssertColorNear(new Color(100, 80, 0), fixture.Sample(pixels, 28, 8));
        AssertColorNear(new Color(100, 50, 25), fixture.Sample(pixels, 48, 8));
        AssertColorNear(new Color(178, 153, 128), fixture.Sample(pixels, 68, 8));
    }

    private static DrawPath RectanglePath(float x, float y, float width, float height) =>
        new DrawPathBuilder()
            .MoveTo(new DrawPoint(x, y))
            .LineTo(new DrawPoint(x + width, y))
            .LineTo(new DrawPoint(x + width, y + height))
            .LineTo(new DrawPoint(x, y + height))
            .Close()
            .Build();

    private static void AssertColorNear(Color expected, Color actual)
    {
        Assert.InRange(Math.Abs(actual.R - expected.R), 0, 3);
        Assert.InRange(Math.Abs(actual.G - expected.G), 0, 3);
        Assert.InRange(Math.Abs(actual.B - expected.B), 0, 3);
        Assert.InRange(Math.Abs(actual.A - expected.A), 0, 3);
    }
}

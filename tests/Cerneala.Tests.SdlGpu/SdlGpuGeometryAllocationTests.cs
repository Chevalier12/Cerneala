using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuGeometryAllocationTests
{
    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void WarmGeometryUsesCurrentPaintTransformAndCompositingState(float scale)
    {
        DrawRect bounds = new(8.125f, 8.25f, 80, 60);
        DrawCommandList CreateCommands(bool changed)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.PushTransform(changed
                ? Matrix3x2.CreateRotation(0.07f) * Matrix3x2.CreateTranslation(17, 9)
                : Matrix3x2.Identity));
            commands.Add(DrawCommand.PushOpacity(changed ? 0.713f : 1));
            commands.Add(DrawCommand.FillEllipse(bounds,
                new Cerneala.UI.Media.SolidColorBrush(changed ? new Color(123, 67, 189, 127) : Color.White, 0.81f),
                opacity: changed ? 0.49f : 1));
            commands.Add(DrawCommand.DrawEllipse(bounds,
                new DrawPen(new Cerneala.UI.Media.SolidColorBrush(changed ? new Color(31, 199, 137, 183) : Color.White), 2)));
            commands.Add(DrawCommand.PopOpacity());
            commands.Add(DrawCommand.PopTransform());
            return commands;
        }

        Color[] changedPixels;
        DrawCommandList initial = CreateCommands(false);
        DrawCommandList changed = CreateCommands(true);
        using (SdlDrawingFixture warmed = new(256, 192, coordinateScale: scale))
        {
            Color[] original = warmed.Render(initial);
            for (int frame = 0; frame < 8; frame++) warmed.Render(initial);
            changedPixels = warmed.Render(changed);
            Assert.False(original.SequenceEqual(changedPixels));
        }
        using SdlDrawingFixture fresh = new(256, 192, coordinateScale: scale);
        Assert.Equal(fresh.Render(changed), changedPixels);
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmShapeReplayDoesNotRetessellateUnchangedGeometry(bool stroke)
    {
        using SdlDrawingFixture fixture = new(320, 240, coordinateScale: 1.25f);
        DrawRect bounds = new(8.125f, 8.25f, 200, 140);
        DrawCommandList commands = new();
        commands.Add(stroke
            ? DrawCommand.DrawEllipse(bounds, Color.White, 2)
            : DrawCommand.FillEllipse(bounds, Color.White));
        DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
        Color[] reference = fixture.Render(commands);
        long allocated = 0;
        const int measuredFrames = 16;
        for (int frame = 0; frame < 8 + measuredFrames; frame++)
        {
            fixture.Session.BeginFrame(Color.Black);
            try
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                fixture.Backend.Render(commands, in context);
                long delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame >= 8) allocated += delta;
            }
            finally { fixture.Session.CompleteFrame(present: false); }
        }

        Assert.Equal(reference, fixture.Render(commands));
        Assert.True(allocated <= measuredFrames * 8_192,
            $"Unchanged {(stroke ? "stroke" : "fill")} replay allocated {allocated / measuredFrames} bytes/frame; " +
            "the warmed backend must reuse geometry rather than rebuild its point/index arrays.");
    }

    [SdlNativeFact]
    public void WarmGeometryUploadsDoNotAllocateAcrossTheFrameRing()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        SdlGpuVertex[] vertices =
        [
            new(Vector2.Zero, Vector2.Zero, Vector4.One),
            new(Vector2.UnitX, Vector2.UnitX, Vector4.One),
            new(Vector2.UnitY, Vector2.UnitY, Vector4.One)
        ];
        int[] indices = [0, 1, 2];
        long allocated = 0;
        for (int frame = 0; frame < 24; frame++)
        {
            fixture.Session.BeginFrame(Color.Transparent);
            try
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                SdlGpuGeometryBinding binding = fixture.Session.GeometryUploadArena.UploadGeometry(
                    fixture.Session, vertices, indices);
                long delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame >= 8) allocated += delta;
                Assert.NotEqual(0, binding.VertexBuffer);
                Assert.NotEqual(0, binding.IndexBuffer);
                Assert.Equal(0u, binding.VertexOffset);
                Assert.Equal(0u, binding.IndexOffset);
            }
            finally { fixture.Session.CompleteFrame(present: false); }
        }
        Assert.Equal(0, allocated);
    }
}

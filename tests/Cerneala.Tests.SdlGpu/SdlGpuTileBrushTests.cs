using System.Numerics;
using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuTileBrushTests
{
    private static readonly DrawRect Tile = new(0, 0, 10, 10);

    [Theory]
    [InlineData(DrawTileMode.Tile, 30f, 10f)]
    [InlineData(DrawTileMode.Tile, 25f, 10f)]
    [InlineData(DrawTileMode.Tile, 10f, 25f)]
    [InlineData(DrawTileMode.FlipX, 25f, 10f)]
    [InlineData(DrawTileMode.FlipY, 10f, 25f)]
    [InlineData(DrawTileMode.FlipXY, 25f, 25f)]
    public void EveryTileKeepsTheContentScaleWhenTheLastTileIsPartial(
        DrawTileMode tileMode, float boundsWidth, float boundsHeight)
    {
        DrawingDrawBrushDescriptor descriptor = new(
            [DrawCommand.FillRectangle(Tile, Color.White)], Tile, DrawBrushStretch.Fill,
            DrawBrushAlignmentX.Center, DrawBrushAlignmentY.Center, Tile, null, tileMode, 1);
        DrawCommandList output = Record(descriptor, new DrawRect(0, 0, boundsWidth, boundsHeight));

        Matrix3x2[] tileTransforms = output
            .Where(command => command.Kind == DrawCommandKind.PushTransform)
            .Skip(1) // the outer bounds/scale transform
            .Select(command => command.Transform)
            .ToArray();
        Assert.True(
            tileTransforms.All(transform => MathF.Abs(transform.M11) == 1f && MathF.Abs(transform.M22) == 1f),
            "Tile transforms (M11, M22): " + string.Join("; ", tileTransforms.Select(t => $"({t.M11}, {t.M22})")));
        AssertTileClipsCoverBounds(output, boundsWidth, boundsHeight);
    }

    [Theory]
    [InlineData(DrawTileMode.Tile)]
    [InlineData(DrawTileMode.FlipXY)]
    public void EveryImageTileKeepsTheViewportSizeWhenTheLastTileIsPartial(DrawTileMode tileMode)
    {
        using SdlGpuImage image = new(10, 10, Enumerable.Repeat((byte)255, 10 * 10 * 4).ToArray());
        ImageDrawBrushDescriptor descriptor = new(image, null, DrawBrushStretch.Fill,
            DrawBrushAlignmentX.Center, DrawBrushAlignmentY.Center, Tile, null, tileMode, 1);
        DrawCommandList output = Record(descriptor, new DrawRect(0, 0, 25, 25));

        DrawRect[] destinations = output
            .Where(command => command.Kind == DrawCommandKind.DrawImage)
            .Select(command => command.Rect)
            .ToArray();
        Assert.Equal(9, destinations.Length);
        Assert.True(
            destinations.All(destination => destination.Width == 10f && destination.Height == 10f),
            "Image destinations: " + string.Join("; ", destinations));
        AssertTileClipsCoverBounds(output, 25, 25);
    }

    private static void AssertTileClipsCoverBounds(DrawCommandList output, float boundsWidth, float boundsHeight)
    {
        // The edge tiles are cropped by the painted bounds, not resized.
        foreach (DrawRect clip in output.Where(command => command.Kind == DrawCommandKind.PushClip)
            .Select(command => command.Rect))
        {
            Assert.Equal(MathF.Min(10f, boundsWidth - clip.X), clip.Width);
            Assert.Equal(MathF.Min(10f, boundsHeight - clip.Y), clip.Height);
        }
    }

    private static DrawCommandList Record(TileDrawBrushDescriptor descriptor, DrawRect bounds)
    {
        var record = typeof(SdlGpuDrawingBackend)
            .GetMethod("RecordTileBrush", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Action<DrawCommandList, TileDrawBrushDescriptor, DrawRect, float>>();
        DrawCommandList output = new();
        record(output, descriptor, bounds, 1f);
        return output;
    }
}

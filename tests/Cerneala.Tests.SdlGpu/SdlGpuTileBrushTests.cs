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

    [Theory]
    [InlineData(DrawTileMode.None)]
    [InlineData(DrawTileMode.Tile)]
    public void TheBaseTileSitsAtTheOffsetViewport(DrawTileMode tileMode)
    {
        DrawCommandList output = Record(Descriptor(new DrawRect(5, 0, 10, 10), tileMode), new DrawRect(0, 0, 30, 10));

        float[] origins = TileTransforms(output).Select(transform => transform.M31).ToArray();
        DrawRect[] clips = Clips(output);
        Assert.Contains(5f, origins);
        Assert.Contains(new DrawRect(5, 0, 10, 10), clips);
        if (tileMode == DrawTileMode.Tile)
        {
            Assert.Equal([-5f, 5f, 15f, 25f], origins);
            Assert.Equal(
                [new DrawRect(0, 0, 5, 10), new DrawRect(5, 0, 10, 10), new DrawRect(15, 0, 10, 10), new DrawRect(25, 0, 5, 10)],
                clips);
        }
    }

    [Fact]
    public void FlipParityCountsFromTheOffsetViewport()
    {
        DrawCommandList output = Record(
            Descriptor(new DrawRect(5, 3, 10, 10), DrawTileMode.FlipXY), new DrawRect(0, 0, 30, 20));

        // Mirrored content is translated to the tile's far edge, so read the tile from the clip.
        (DrawRect Clip, Matrix3x2 Transform)[] tiles = Clips(output).Zip(TileTransforms(output)).ToArray();
        (DrawRect Clip, Matrix3x2 Transform) baseTile = Assert.Single(tiles, tile => tile.Clip == new DrawRect(5, 3, 10, 10));
        Assert.Equal((1f, 1f), (baseTile.Transform.M11, baseTile.Transform.M22));
        Assert.Equal((-1f, 1f), TileScale(tiles, new DrawRect(0, 3, 5, 10)));
        Assert.Equal((-1f, 1f), TileScale(tiles, new DrawRect(15, 3, 10, 10)));
        Assert.Equal((1f, -1f), TileScale(tiles, new DrawRect(5, 0, 10, 3)));
        Assert.Equal((1f, -1f), TileScale(tiles, new DrawRect(5, 13, 10, 7)));
        Assert.Equal((-1f, -1f), TileScale(tiles, new DrawRect(15, 13, 10, 7)));
    }

    [Theory]
    [InlineData(-23f, -7f)]
    [InlineData(47f, 31f)]
    public void AnyViewportPositionFillsThePaintedBounds(float viewportX, float viewportY)
    {
        DrawRect bounds = new(2, 4, 30, 20);
        DrawCommandList output = Record(
            Descriptor(new DrawRect(viewportX, viewportY, 10, 10), DrawTileMode.FlipX), bounds);

        DrawRect[] clips = Clips(output);
        Assert.All(clips, clip => Assert.True(
            clip.Width > 0 && clip.Height > 0 && clip.X >= bounds.X && clip.Y >= bounds.Y &&
            clip.Right <= bounds.Right && clip.Bottom <= bounds.Bottom, "Clip outside the bounds: " + clip));
        Assert.Equal(bounds.Width * bounds.Height, clips.Sum(clip => clip.Width * clip.Height), 3);
        float[] edgesX = clips.SelectMany(clip => new[] { clip.X, clip.Right }).Distinct().Order().ToArray();
        Assert.Equal(bounds.X, edgesX[0]);
        Assert.Equal(bounds.Right, edgesX[^1]);
    }

    private static DrawingDrawBrushDescriptor Descriptor(DrawRect viewport, DrawTileMode tileMode) => new(
        [DrawCommand.FillRectangle(Tile, Color.White)], Tile, DrawBrushStretch.Fill,
        DrawBrushAlignmentX.Center, DrawBrushAlignmentY.Center, viewport, null, tileMode, 1);

    private static Matrix3x2[] TileTransforms(DrawCommandList output) => output
        .Where(command => command.Kind == DrawCommandKind.PushTransform)
        .Skip(1) // the outer bounds/scale transform
        .Select(command => command.Transform)
        .ToArray();

    private static DrawRect[] Clips(DrawCommandList output) => output
        .Where(command => command.Kind == DrawCommandKind.PushClip)
        .Select(command => command.Rect)
        .ToArray();

    private static (float, float) TileScale((DrawRect Clip, Matrix3x2 Transform)[] tiles, DrawRect clip)
    {
        Matrix3x2 transform = Assert.Single(tiles, tile => tile.Clip == clip).Transform;
        return (transform.M11, transform.M22);
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

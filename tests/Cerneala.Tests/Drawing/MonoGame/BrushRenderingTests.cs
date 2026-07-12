using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.UI.Media;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class BrushRenderingTests
{
    [Fact]
    public void LinearGradientSamplingInterpolatesColorAndOpacity()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            [new GradientStop(0, new Color(0, 0, 0, 255)), new GradientStop(1, new Color(200, 100, 50, 255))],
            0.5f);

        Color sampled = MonoGameDrawingBackend.SampleBrushForDiagnostics(brush, new DrawPoint(5, 0), 0.5f);

        Assert.Equal(new Color(100, 50, 25, 64), sampled);
    }

    [Fact]
    public void RadialGradientSamplingClampsOutsideRadius()
    {
        RadialGradientBrush brush = new(
            new DrawPoint(5, 5),
            5,
            5,
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);

        Assert.Equal(Color.White, MonoGameDrawingBackend.SampleBrushForDiagnostics(brush, new DrawPoint(5, 5)));
        Assert.Equal(Color.Black, MonoGameDrawingBackend.SampleBrushForDiagnostics(brush, new DrawPoint(20, 20)));
    }

    [Fact]
    public void RadialGradientSamplingUsesPaintBoundsLocalCoordinates()
    {
        RadialGradientBrush brush = new(
            new DrawPoint(5, 5),
            5,
            5,
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
        DrawRect bounds = new(100, 200, 10, 10);

        Color sampled = MonoGameDrawingBackend.SampleBrushInBoundsForDiagnostics(
            brush,
            bounds,
            new DrawPoint(105, 205));

        Assert.Equal(Color.White, sampled);
    }

    [Fact]
    public void DegenerateLinearGradientUsesLastStop()
    {
        LinearGradientBrush brush = new(
            new DrawPoint(5, 5),
            new DrawPoint(5, 5),
            [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);

        Assert.Equal(Color.Black, MonoGameDrawingBackend.SampleBrushForDiagnostics(brush, new DrawPoint(5, 5)));
    }

    [Fact]
    public void UniformTilePreservesAspectRatioAndAlignment()
    {
        DrawRect fitted = MonoGameDrawingBackend.FitTileForDiagnostics(
            new DrawRect(0, 0, 100, 100),
            200,
            100,
            DrawBrushStretch.Uniform,
            DrawBrushAlignmentX.Right,
            DrawBrushAlignmentY.Bottom);

        Assert.Equal(new DrawRect(0, 50, 100, 50), fitted);
    }

    [Fact]
    public void VisualBrushGraphRejectsSelfReference()
    {
        Cerneala.UI.Controls.Shapes.Rectangle source = new();
        source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
        VisualBrush brush = new(source);
        source.Fill = brush;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => MonoGameDrawingBackend.ValidateBrushGraphForDiagnostics(brush));

        Assert.Contains("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}

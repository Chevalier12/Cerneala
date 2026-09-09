using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using PathShape = Cerneala.UI.Controls.Shapes.Path;
using RectangleShape = Cerneala.UI.Controls.Shapes.Rectangle;
using Matrix = System.Numerics.Matrix3x2;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class ShapePrimitiveRenderingTests
{
    private const string CurveData = "M0 0 C10 -5 30 -5 40 0 A20 15 0 0 1 40 30 Q20 40 0 30 Z M10 10L30 10L30 20L10 20Z";
    private static readonly DrawPoint[] Points = [new(0, 0), new(36, 0), new(36, 26)];

    [SdlNativeTheory]
    [InlineData(0, 1f)]
    [InlineData(1, 1f)]
    [InlineData(2, 1f)]
    [InlineData(3, 1f)]
    [InlineData(4, 1f)]
    [InlineData(5, 1f)]
    [InlineData(6, 1f)]
    [InlineData(7, 1f)]
    [InlineData(0, 1.25f)]
    [InlineData(1, 1.25f)]
    [InlineData(2, 1.25f)]
    [InlineData(3, 1.25f)]
    [InlineData(4, 1.25f)]
    [InlineData(5, 1.25f)]
    [InlineData(6, 1.25f)]
    [InlineData(7, 1.25f)]
    public void RetainedShapesMatchDirectDrawingWithAffineTransformsAndOpacity(int scene, float coordinateScale)
    {
        using SdlDrawingFixture fixture = new(160, 160, useMultisampling: true, coordinateScale: coordinateScale);
        SolidColorBrush fill = new(new Color(60, 160, 220));
        SolidColorBrush stroke = new(new Color(220, 80, 40));
        const float opacity = 0.65f;
        Shape shape = scene switch
        {
            0 => new RectangleShape(),
            1 => new Ellipse(),
            2 => new Line { EndPoint = new DrawPoint(36, 0) },
            3 => new Line { EndPoint = new DrawPoint(0, 26) },
            4 => new Polyline { Points = Points },
            5 => new Polygon { Points = Points },
            6 => new RectangleShape { RadiusX = 12, RadiusY = 5 },
            _ => new PathShape { Data = PathGeometry.Parse(CurveData) }
        };
        shape.Fill = scene is 2 or 3 ? null : fill;
        shape.Stroke = stroke;
        shape.StrokeThickness = 2;
        shape.Opacity = opacity;
        shape.FillRule = DrawFillRule.EvenOdd;
        shape.ClipToBounds = scene == 7;
        shape.RenderTransformOrigin = new LayoutPoint(0, 0);
        var uiTransform = Matrix3x2.Multiply(
            Matrix3x2.Multiply(Matrix3x2.CreateScale(1.2f, 0.8f), Matrix3x2.CreateSkew(0.1f, 0.05f)),
            Matrix3x2.CreateRotation(0.35f));
        shape.RenderTransform = new Transform(uiTransform);
        Matrix transform = Matrix.CreateScale(1.2f, 0.8f) * Matrix.CreateSkew(0.1f, 0.05f) * Matrix.CreateRotation(0.35f);
        UIRoot root = new();
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(new LayoutRect(45, 45, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "shape-native-contract");
        root.ProcessFrame();
        Color[] actual = fixture.Render(root.RetainedRenderer.Commit(root));

        DrawCommandList expectedCommands = new();
        // Native rectangle fills snap their input edges before the drawing transform.
        // Keep the same arranged-coordinate space instead of moving those edges into a matrix.
        expectedCommands.Add(DrawCommand.PushTransform(scene == 0
            ? Matrix.CreateTranslation(-45, -45) * transform * Matrix.CreateTranslation(45, 45)
            : transform * Matrix.CreateTranslation(45, 45)));
        DrawRect rect = scene == 0 ? new(45, 45, 40, 30) : new(0, 0, 40, 30);
        if (shape.ClipToBounds)
        {
            expectedCommands.Add(DrawCommand.PushClip(rect));
        }
        DrawPen pen = new(stroke, 2);
        switch (scene)
        {
            case 0:
                expectedCommands.Add(DrawCommand.FillRectangle(rect, fill, opacity));
                expectedCommands.Add(DrawCommand.DrawRectangle(rect, pen, opacity));
                break;
            case 1:
                expectedCommands.Add(DrawCommand.FillEllipse(rect, fill, opacity));
                expectedCommands.Add(DrawCommand.DrawEllipse(rect, pen, opacity));
                break;
            case 2:
                expectedCommands.Add(DrawCommand.DrawLine(default, new DrawPoint(36, 0), pen, opacity));
                break;
            case 3:
                expectedCommands.Add(DrawCommand.DrawLine(default, new DrawPoint(0, 26), pen, opacity));
                break;
            default:
                DrawPath path = scene switch
                {
                    4 => DrawPathFactory.Polyline(Points),
                    5 => DrawPathFactory.Polygon(Points),
                    6 => DrawPathParser.ParseSvg("M12 0L28 0A12 5 0 0 1 40 5L40 25A12 5 0 0 1 28 30L12 30A12 5 0 0 1 0 25L0 5A12 5 0 0 1 12 0Z"),
                    _ => DrawPathParser.ParseSvg(CurveData)
                };
                expectedCommands.Add(DrawCommand.FillPath(path, path.Bounds, path.Bounds, fill, DrawFillRule.EvenOdd, opacity));
                expectedCommands.Add(DrawCommand.DrawPath(path, path.Bounds, path.Bounds, pen, opacity));
                break;
        }
        if (shape.ClipToBounds)
        {
            expectedCommands.Add(DrawCommand.PopClip());
        }
        expectedCommands.Add(DrawCommand.PopTransform());
        Color[] expected = fixture.Render(expectedCommands);

        Assert.Contains(expected, pixel => pixel.R > 20 || pixel.G > 20 || pixel.B > 20);
        int maximumDelta = 0;
        for (int index = 0; index < expected.Length; index++)
        {
            maximumDelta = Math.Max(maximumDelta, Math.Max(
                Math.Abs(actual[index].R - expected[index].R), Math.Max(
                    Math.Abs(actual[index].G - expected[index].G), Math.Abs(actual[index].B - expected[index].B))));
        }
        Assert.True(maximumDelta <= 2, $"Scene {scene}, scale {coordinateScale}: maximum RGB delta {maximumDelta}/255.");
    }

    [SdlNativeTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void RelocatedCacheMatchesFreshRecordingAtFractionalDpi(int scene)
    {
        using SdlDrawingFixture fixture = new(120, 120, useMultisampling: true, coordinateScale: 1.25f);
        SolidColorBrush white = new(Color.White);
        UIElement shape = scene switch
        {
            0 => new RectangleShape { Fill = white },
            1 => new RectangleShape { Fill = white, RadiusX = 7, RadiusY = 3 },
            2 => new Line { EndPoint = new DrawPoint(25, 0), Stroke = white, StrokeThickness = 2 },
            3 => new Line { EndPoint = new DrawPoint(0, 15), Stroke = white, StrokeThickness = 2 },
            4 => new Polygon { Points = Points, Fill = white },
            5 => new PathShape { Data = PathGeometry.Parse(CurveData), Fill = white },
            6 => new Ellipse { Fill = white },
            _ => new LocalAffineClipElement()
        };
        UIRoot root = new();
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(new LayoutRect(40, 40, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "prepare-relocation");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
        ElementRenderCache localCache = root.RetainedRenderCache.GetElementCache(shape);
        long version = localCache.RenderVersion;

        shape.Arrange(new ArrangeContext(new LayoutRect(41, 41, 30, 20)));
        Assert.False(shape.DirtyState.Has(InvalidationFlags.Render));
        new DrawCommandListBuilder().Build(root, root.RetainedRenderCache, new RenderCounters());
        Color[] reused = fixture.Render(root.RetainedRenderCache.RootCommands);
        Assert.Equal(version, localCache.RenderVersion);
        Assert.Equal(new LayoutRect(40, 40, 30, 20), localCache.ContentBounds);

        localCache.Ensure(shape, new RenderCounters(), forceRebuild: true);
        new DrawCommandListBuilder().Build(root, root.RetainedRenderCache, new RenderCounters());
        Color[] rebuilt = fixture.Render(root.RetainedRenderCache.RootCommands);
        int maximumDelta = 0;
        for (int index = 0; index < rebuilt.Length; index++)
        {
            maximumDelta = Math.Max(maximumDelta, Math.Abs(rebuilt[index].R - reused[index].R));
        }
        Assert.True(maximumDelta <= 2, $"Relocation scene {scene}: maximum delta {maximumDelta}/255.");
    }

    private sealed class LocalAffineClipElement : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            LayoutRect bounds = context.Bounds;
            using var transform = context.DrawingContext.Transform(
                Matrix.CreateTranslation(-bounds.X, -bounds.Y) *
                Matrix.CreateRotation(0.2f) * Matrix.CreateTranslation(bounds.X, bounds.Y));
            DrawPath clipPath = DrawPathFactory.Polygon(
            [
                new DrawPoint(bounds.X, bounds.Y),
                new DrawPoint(bounds.X + bounds.Width, bounds.Y),
                new DrawPoint(bounds.X, bounds.Y + bounds.Height)
            ]);
            using var clip = context.DrawingContext.Clip(clipPath);
            context.DrawingContext.FillRectangle(
                new DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height), Color.White);
        }
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Controls.Shapes;

public sealed class ShapeRetainedReuseTests
{
    [Fact]
    public void TransformAndOpacityChangesReuseShapeCommandsAndNativeGeometryFor256Frames()
    {
        UIRoot root = new(100, 100);
        Polygon polygon = new()
        {
            Points = [new(0, 0), new(30, 0), new(30, 20)],
            Fill = new SolidColorBrush(Color.White),
            Stroke = new SolidColorBrush(Color.Black)
        };
        root.VisualChildren.Add(polygon);
        root.ProcessFrame();
        DrawPath path = Assert.Single(root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind == DrawCommandKind.DrawPath)).Path!;
        var cache = root.RetainedRenderCache.GetElementCache(polygon);
        int version = cache.RenderVersion;

        for (int frame = 0; frame < 256; frame++)
        {
            polygon.Rotation = frame * 0.01f;
            polygon.Opacity = (frame & 1) == 0 ? 0.25f : 0.75f;
            root.ProcessFrame();
            DrawCommandList commands = root.RetainedRenderer.Commit(root);
            DrawCommand stroke = Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawPath));
            Assert.Same(path, stroke.Path);
            Assert.Equal(polygon.Opacity, stroke.BrushOpacity);
            Assert.Equal(version, cache.RenderVersion);
            new DrawCommandStateAnalyzer().Analyze(commands);
        }
    }
}

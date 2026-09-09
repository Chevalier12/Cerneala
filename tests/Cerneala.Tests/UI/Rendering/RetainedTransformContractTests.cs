using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Transform = Cerneala.UI.Media.Transform;
using UiMatrix = Cerneala.UI.Media.Matrix3x2;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RetainedTransformContractTests
{
    [Fact]
    public void RotationPreservesRectangleCornersInsteadOfDrawingItsBoundingBox()
    {
        RenderingTestElement element = new(Color.White)
        {
            RenderTransformOrigin = new LayoutPoint(0, 0),
            RenderTransform = new Transform(UiMatrix.CreateRotation(MathF.PI / 4))
        };

        DrawCommandList commands = Compose(element);
        AssertCorners(commands, Matrix3x2.CreateRotation(MathF.PI / 4));
    }

    [Fact]
    public void ParentAndChildTransformsComposeWithoutLosingRotationOrSkew()
    {
        UIElement parent = new()
        {
            RenderTransformOrigin = new LayoutPoint(0, 0),
            RenderTransform = new Transform(UiMatrix.CreateRotation(MathF.PI / 4))
        };
        RenderingTestElement child = new(Color.White)
        {
            RenderTransformOrigin = new LayoutPoint(0, 0),
            RenderTransform = new Transform(UiMatrix.CreateSkew(0.2f, 0.1f))
        };
        parent.VisualChildren.Add(child);

        DrawCommandList commands = Compose(parent);
        AssertCorners(commands,
            Matrix3x2.CreateSkew(0.2f, 0.1f) * Matrix3x2.CreateRotation(MathF.PI / 4));
    }

    [Fact]
    public void ElementTransformWrapsRatherThanReordersDrawingLocalTransform()
    {
        LocalTransformElement element = new()
        {
            RenderTransformOrigin = new LayoutPoint(0, 0),
            RenderTransform = new Transform(UiMatrix.CreateTranslation(10, 20))
        };

        DrawCommandList commands = Compose(element);
        AssertCorners(commands,
            Matrix3x2.CreateRotation(MathF.PI / 4) * Matrix3x2.CreateTranslation(10, 20));
    }

    private static DrawCommandList Compose(UIElement element)
    {
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        Prepare(element);
        new DrawCommandListBuilder().Build(element, cache, counters);
        return cache.RootCommands;

        void Prepare(UIElement current)
        {
            cache.GetElementCache(current).Ensure(current, counters, forceRebuild: true);
            foreach (UIElement child in current.VisualChildren)
            {
                Prepare(child);
            }
        }
    }

    private static void AssertCorners(DrawCommandList commands, Matrix3x2 expected)
    {
        DrawCommandStateAnalysis state = new DrawCommandStateAnalyzer().Analyze(commands);
        int index = Assert.Single(Enumerable.Range(0, commands.Count)
            .Where(index => commands[index].Kind == DrawCommandKind.FillRectangle));
        DrawRect rect = commands[index].Rect;
        Vector2[] actualCorners =
        [
            new(rect.X, rect.Y), new(rect.Right, rect.Y),
            new(rect.Right, rect.Bottom), new(rect.X, rect.Bottom)
        ];
        Vector2[] originalCorners = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        for (int corner = 0; corner < actualCorners.Length; corner++)
        {
            Vector2 actual = Vector2.Transform(actualCorners[corner], state.Entries[index].Transform);
            Vector2 target = Vector2.Transform(originalCorners[corner], expected);
            Assert.InRange(Vector2.Distance(actual, target), 0, 0.0001f);
        }
    }

    private sealed class LocalTransformElement : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            using var scope = context.DrawingContext.Transform(Matrix3x2.CreateRotation(MathF.PI / 4));
            context.DrawingContext.FillRectangle(new DrawRect(0, 0, 1, 1), Color.White);
        }
    }
}

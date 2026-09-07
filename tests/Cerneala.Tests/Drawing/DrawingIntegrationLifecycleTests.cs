using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingIntegrationLifecycleTests
{
    [Fact]
    [Trait("PlanStage", "7")]
    public void EveryCommandKindUsesCentralMetadataInsidePrismAndNestedSurfaces()
    {
        TestImage image = new(16, 16);
        TestFont font = new();
        SolidColorBrush brush = new(Color.White);
        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(0, 0))
            .LineTo(new DrawPoint(8, 0))
            .LineTo(new DrawPoint(8, 8))
            .Close()
            .Build();
        DrawPen pen = new(brush, 1);
        DrawMesh2D mesh = Triangle(1, 1, image);
        DrawPointBatch points = new([new DrawPoint(2, 2)], Color.White, 2);
        DrawLineBatch lines = new([
            new DrawLineSegment2D(new DrawPoint(1, 1), new DrawPoint(5, 5), Color.White)
        ]);
        DrawSpriteBatch sprites = new(image, [new DrawSprite2D(new DrawRect(1, 1, 4, 4))]);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("layout", font, 10, brush)
            .Build(new DrawTextLayoutOptions(maxWidth: 40));
        PrismDrawScope prismScope = PrismTestData.Scope(
            PrismTestData.Composition("Stage7", PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 64, 64));
        DrawCommandList commands = new();

        commands.Add(DrawCommand.BeginPrism(prismScope));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawRectangle(new DrawRect(1, 1, 4, 4), pen));
        commands.Add(DrawCommand.FillRoundedRectangle(new DrawRect(1, 1, 4, 4), new DrawCornerRadius(1), Color.White));
        commands.Add(DrawCommand.DrawRoundedRectangle(new DrawRect(1, 1, 4, 4), new DrawCornerRadius(1), pen));
        commands.Add(DrawCommand.FillEllipse(new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawEllipse(new DrawRect(1, 1, 4, 4), pen));
        commands.Add(DrawCommand.DrawLine(new DrawPoint(1, 1), new DrawPoint(4, 4), pen));
        commands.Add(DrawCommand.FillPath(path, brush));
        commands.Add(DrawCommand.DrawPath(path, pen));
        commands.Add(DrawCommand.DrawText(new DrawTextRun(font, "text", 10), new DrawPoint(1, 1), brush));
        commands.Add(DrawCommand.DrawTextLayout(layout, new DrawPoint(1, 1)));
        commands.Add(DrawCommand.DrawImage(image, new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawImageQuad(
            image,
            new DrawPoint(1, 1),
            new DrawPoint(5, 1),
            new DrawPoint(5, 5),
            new DrawPoint(1, 5)));
        commands.Add(DrawCommand.DrawNineSlice(image, new DrawRect(1, 1, 8, 8), new DrawInsets(1)));
        commands.Add(DrawCommand.DrawMesh(mesh));
        commands.Add(DrawCommand.DrawPointBatch(points));
        commands.Add(DrawCommand.DrawLineBatch(lines));
        commands.Add(DrawCommand.DrawSpriteBatch(sprites));
        commands.Add(DrawCommand.RenderSurface2D(new TestSurface(), new DrawRect(1, 1, 8, 8), Color.White));
        commands.Add(DrawCommand.EndPrism());
        commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 10, 10)));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PushClip(path));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PushTransform(System.Numerics.Matrix3x2.Identity));
        commands.Add(DrawCommand.PopTransform());
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.PopOpacity());
        commands.Add(DrawCommand.PushBlend(DrawBlendMode.Multiply));
        commands.Add(DrawCommand.PopBlend());
        commands.Add(DrawCommand.PushLayer(new DrawLayerOptions(0.5f, DrawBlendMode.Screen)));
        commands.Add(DrawCommand.PopLayer());

        DrawCommandStateAnalysis state = new DrawCommandStateAnalyzer().Analyze(commands);
        PrismFrameAnalysis prism = new PrismFrameAnalyzer().Analyze(commands);

        Assert.Equal(
            Enum.GetValues<DrawCommandKind>().OrderBy(static kind => kind),
            commands.Select(static command => command.Kind).Distinct().OrderBy(static kind => kind));
        Assert.All(state.Entries, static entry => Assert.NotNull(entry.Metadata));
        Assert.All(
            Enum.GetValues<DrawCommandKind>(),
            static kind => _ = DrawCommandMetadata.IsContextSensitiveKind(kind));
        Assert.Single(prism.Scopes);
        Assert.Contains(
            state.Entries[15].Metadata!.Resources,
            resource => ReferenceEquals(resource, image));
        Assert.Contains(
            state.Entries[19].Metadata!.Resources,
            resource => resource is TestSurface);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void RetainedIdentitySnapshotsPrismValueVersions()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Retained", PrismTestData.Layer(1, "Content")));
        DrawCommand first = DrawCommand.BeginPrism(scope);

        scope.Instance.GetLayerState(new Cerneala.UI.Prism.Definitions.PrismNodeId(1)).Opacity = 0.5f;
        DrawCommand second = DrawCommand.BeginPrism(scope);

        Assert.NotEqual(first.RetainedVersion, second.RetainedVersion);
        Assert.NotEqual(
            DrawCommandMetadata.Create(first).RetainedIdentity,
            DrawCommandMetadata.Create(second).RetainedIdentity);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void FrameTracksAllImageResourcesThroughCentralMetadata()
    {
        TestImage image = new(16, 16);
        ImageTestBrush imageBrush = new(image);
        DrawMesh2D mesh = Triangle(0, 0, image);
        DrawSpriteBatch sprites = new(image, [new DrawSprite2D(new DrawRect(0, 0, 4, 4))]);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("image brush", new TestFont(), 10, imageBrush)
            .Build();
        DrawCommandList commands = new();
        List<IDrawImage> tracked = [];
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 64),
            TimeSpan.Zero,
            tracked.Add);

        frame.DrawImage(image, new DrawRect(0, 0, 4, 4), Color.White);
        frame.DrawMesh(mesh);
        frame.DrawSpriteBatch(sprites);
        frame.DrawTextLayout(layout, new DrawPoint(0, 0));
        frame.Complete();

        Assert.Equal(4, tracked.Count);
        Assert.All(tracked, candidate => Assert.Same(image, candidate));
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void DiagnosticsNameInvalidGeometryAndUnbalancedState()
    {
        ArgumentOutOfRangeException geometry = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DrawMesh2D(
                [
                    new DrawVertex2D(new DrawPoint(0, 0), Color.White),
                    new DrawVertex2D(new DrawPoint(1, 0), Color.White),
                    new DrawVertex2D(new DrawPoint(0, 1), Color.White)
                ],
                [0, 1, 9]));
        DrawCommandList unbalanced = new();
        unbalanced.Add(DrawCommand.PushOpacity(0.5f));
        InvalidOperationException state = Assert.Throws<InvalidOperationException>(
            () => new DrawCommandStateAnalyzer().Analyze(unbalanced));

        Assert.Contains("index", geometry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("command index 0", state.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DrawMesh2D Triangle(int x, int y, IDrawImage? image = null) =>
        new(
            [
                new DrawVertex2D(new DrawPoint(x, y), Color.White),
                new DrawVertex2D(new DrawPoint(x + 4, y), Color.White),
                new DrawVertex2D(new DrawPoint(x, y + 4), Color.White)
            ],
            [0, 1, 2],
            image: image);

    private sealed record TestImage(int Width, int Height) : IDrawImage;

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Stage7Test";

        public float Size => 10;
    }

    private sealed class ImageTestBrush(IDrawImage image) : IDrawBrush
    {
        public DrawBrushKind Kind => DrawBrushKind.Image;

        public float Opacity => 1;

        public Color? SolidColor => null;

        public DrawBrushDescriptor CreateDescriptor() =>
            new ImageDrawBrushDescriptor(
                image,
                SourceIdentity: null,
                DrawBrushStretch.Fill,
                DrawBrushAlignmentX.Center,
                DrawBrushAlignmentY.Center,
                Viewport: null,
                Viewbox: null,
                DrawTileMode.None,
                BrushOpacity: 1);
    }

    private sealed class TestSurface : IRenderSurface2DSource;
}

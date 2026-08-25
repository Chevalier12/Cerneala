using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Microsoft.Xna.Framework.Graphics;

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

    [Fact]
    [Trait("PlanStage", "7")]
    public void RetainedSessionReportsMissesBoundsReuseAndDeterministicDisposal()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using MonoGame.PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        DrawMesh2D mesh = Triangle(2, 2);
        Texture2D surface;
        try
        {
            void Draw(RenderSurface2DFrame frame) => frame.DrawMesh(mesh);

            session.Render(Draw, Color.Black, TimeSpan.Zero);
            Assert.Equal(RenderSurface2DRetainedMissReason.FirstFrame, session.LastRetainedMissReason);
            fixture.Session.GraphicsDevice.SetRenderTarget(null);

            session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(16));
            Assert.Equal(RenderSurface2DRetainedMissReason.None, session.LastRetainedMissReason);
            Assert.Null(session.LastDamageBounds);
            fixture.Session.GraphicsDevice.SetRenderTarget(null);

            mesh = Triangle(10, 2);
            session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(32));
            Assert.Equal(RenderSurface2DRetainedMissReason.CommandPayloadChanged, session.LastRetainedMissReason);
            Assert.Equal(new Microsoft.Xna.Framework.Rectangle(2, 2, 12, 4), session.LastDamageBounds);
            fixture.Session.GraphicsDevice.SetRenderTarget(null);

            for (int frame = 0; frame < 128; frame++)
            {
                session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(48 + frame));
                fixture.Session.GraphicsDevice.SetRenderTarget(null);
            }

            Assert.Equal(2, session.RasterizedFrameCount);
            Assert.Equal(1, session.RetainedCommandCount);
            Assert.Equal(RenderSurface2DRetainedMissReason.None, session.LastRetainedMissReason);

            void DrawScoped(RenderSurface2DFrame frame)
            {
                frame.PushOpacity(0.75f);
                frame.DrawMesh(mesh);
                frame.PopOpacity();
            }

            session.Render(DrawScoped, Color.Black, TimeSpan.FromMilliseconds(200));
            Assert.Equal(
                RenderSurface2DRetainedMissReason.ContextSensitiveCommand,
                session.LastRetainedMissReason);
            Assert.Equal(
                new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 16),
                session.LastDamageBounds);
            Assert.Equal(3, session.RasterizedFrameCount);
            surface = session.Surface;
        }
        finally
        {
            session.Dispose();
        }

        Assert.True(surface.IsDisposed);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void DisposedImageDiagnosticIdentifiesTheResourceBeforeDrawing()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using MonoGame.PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            16,
            16);
        using MonoGameImage image = new(new Texture2D(
            fixture.Session.GraphicsDevice,
            1,
            1));
        image.Dispose();

        ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(
            () => session.Render(
                frame => frame.DrawImage(
                    image,
                    new DrawRect(0, 0, 8, 8),
                    Color.White),
                Color.Black,
                TimeSpan.Zero));

        Assert.Contains(
            nameof(MonoGameImage),
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void OnDemandSurfaceReusesContentAndRecreatesResourcesOnResize()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using MonoGame.PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        UIRoot root = new(32, 32);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.White);
        };
        root.VisualChildren.Add(surface);
        IMonoGameRenderSurface2DSource source = surface;

        Texture2D first = source.ResolveSurface(fixture.Session.GraphicsDevice, 8, 8)!;
        Texture2D unchanged = source.ResolveSurface(fixture.Session.GraphicsDevice, 8, 8)!;
        Texture2D resized = source.ResolveSurface(fixture.Session.GraphicsDevice, 16, 12)!;

        Assert.Same(first, unchanged);
        Assert.Equal(2, drawCount);
        Assert.True(first.IsDisposed);
        Assert.False(resized.IsDisposed);

        Assert.True(root.VisualChildren.Remove(surface));
        Assert.True(resized.IsDisposed);
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

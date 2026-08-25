using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.MonoGame;
using Cerneala.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingImageMeshBatchTests
{
    [Fact]
    public void ImageOptionsPreserveLegacyPayloadAndValidateInputs()
    {
        TestImage image = new(32, 24);
        DrawRect source = new(2, 3, 8, 6);
        DrawCommand command = DrawCommand.DrawImage(
            image,
            new DrawRect(10, 20, 40, 30),
            source,
            Color.HotPink,
            rotation: 0.25f,
            origin: new DrawPoint(4, 3),
            DrawImageFlip.Horizontal,
            layerDepth: 0.75f);

        Assert.Equal(source, command.ImageSource);
        Assert.Equal(Color.HotPink, command.ImageOptions!.Tint);
        Assert.Equal(0.25f, command.ImageOptions.Rotation);
        Assert.Equal(DrawImageFlip.Horizontal, command.ImageOptions.Flip);
        Assert.Equal(0.75f, command.ImageOptions.LayerDepth);
        Assert.Equal(DrawSamplingMode.Linear, command.ImageOptions.Sampling);
        Assert.Equal(DrawAddressMode.Clamp, command.ImageOptions.AddressMode);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DrawImageOptions(opacity: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DrawImageOptions(layerDepth: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DrawCommand.DrawImage(
                image,
                new DrawRect(0, 0, 10, 10),
                new DrawImageOptions(source: new DrawRect(30, 0, 4, 4))));
    }

    [Fact]
    public void ImageQuadUsesExactlyTwoTrianglesAndExplicitUvs()
    {
        TestImage image = new(16, 16);
        DrawCommand command = DrawCommand.DrawImageQuad(
            image,
            new DrawVertex2D(new DrawPoint(1, 2), Color.White, new DrawPoint(0.1f, 0.2f)),
            new DrawVertex2D(new DrawPoint(11, 3), Color.White, new DrawPoint(0.9f, 0.2f)),
            new DrawVertex2D(new DrawPoint(10, 13), Color.White, new DrawPoint(0.9f, 0.8f)),
            new DrawVertex2D(new DrawPoint(0, 12), Color.White, new DrawPoint(0.1f, 0.8f)),
            DrawSamplingMode.Point,
            DrawAddressMode.Wrap);

        Assert.Equal(DrawCommandKind.DrawImageQuad, command.Kind);
        Assert.Equal([0, 1, 2, 0, 2, 3], command.Mesh!.Indices);
        Assert.Equal(new DrawPoint(0.1f, 0.2f), command.Mesh.Vertices[0].TextureCoordinate);
        Assert.Equal(new DrawRect(0, 2, 11, 11), command.Mesh.Bounds);
        Assert.Equal(DrawSamplingMode.Point, command.ImageOptions!.Sampling);
        Assert.Equal(DrawAddressMode.Wrap, command.ImageOptions.AddressMode);
    }

    [Fact]
    public void NineSliceFitsCornersProportionallyAndIsFractionallyDeterministic()
    {
        TestImage image = new(10, 10);
        DrawRect destination = new(0.25f, 0.5f, 5.5f, 3.5f);
        DrawInsets insets = new(4, 3, 4, 3);

        DrawCommand first = DrawCommand.DrawNineSlice(
            image,
            destination,
            insets,
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        DrawCommand second = DrawCommand.DrawNineSlice(
            image,
            destination,
            insets,
            new DrawImageOptions(sampling: DrawSamplingMode.Point));

        Assert.Equal(16, first.Mesh!.Vertices.Count);
        Assert.Equal(54, first.Mesh.Indices.Count);
        Assert.Equal(3f, first.Mesh.Vertices[1].Position.X);
        Assert.Equal(3f, first.Mesh.Vertices[2].Position.X);
        Assert.Equal(2.25f, first.Mesh.Vertices[4].Position.Y);
        Assert.Equal(2.25f, first.Mesh.Vertices[8].Position.Y);
        Assert.Equal(
            first.Mesh.Vertices.Select(vertex => vertex.Position),
            second.Mesh!.Vertices.Select(vertex => vertex.Position));
        Assert.Throws<ArgumentException>(() => DrawCommand.DrawNineSlice(
            image,
            destination,
            new DrawInsets(6)));
    }

    [Fact]
    public void MeshCopiesInputsAndValidatesTopologyIndicesAndImageLifetimeShape()
    {
        DrawVertex2D[] vertices = TriangleVertices();
        int[] indices = [0, 1, 2];
        DrawMesh2D mesh = new(vertices, indices);
        vertices[0] = new DrawVertex2D(new DrawPoint(99, 99), Color.Black);
        indices[0] = 2;

        Assert.Equal(new DrawPoint(1, 1), mesh.Vertices[0].Position);
        Assert.Equal(0, mesh.Indices[0]);
        Assert.Equal(new DrawRect(1, 1, 9, 8), mesh.Bounds);
        Assert.Throws<ArgumentException>(() => new DrawMesh2D(
            TriangleVertices(),
            [0, 1],
            DrawPrimitiveTopology.TriangleList));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawMesh2D(
            TriangleVertices(),
            [0, 1, 3]));
        Assert.Throws<ArgumentException>(() => new DrawMesh2D(
            TriangleVertices(),
            [0, 1, 2],
            image: new TestImage(0, 1)));
    }

    [Fact]
    public void ImmutableBatchesRecordOneCommandAndCarryDistinctVersionsAndBounds()
    {
        DrawPoint[] sourcePoints = [new DrawPoint(5, 5), new DrawPoint(10, 10)];
        DrawPointBatch points = new(sourcePoints, Color.White, 2);
        sourcePoints[0] = new DrawPoint(100, 100);
        DrawLineBatch lines = new(
            [new DrawLineSegment2D(new DrawPoint(0, 0), new DrawPoint(10, 0), Color.White, 2)]);
        TestImage image = new(8, 8);
        DrawSpriteBatch sprites = new(
            image,
            [new DrawSprite2D(new DrawRect(2, 3, 8, 8))]);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.DrawPointBatch(points);
        drawing.DrawLineBatch(lines);
        drawing.DrawSpriteBatch(sprites);

        Assert.Equal(new DrawPoint(5, 5), points.Points[0]);
        Assert.All([points.Version, lines.Version, sprites.Version], version => Assert.True(version > 0));
        Assert.Equal(3, new[] { points.Version, lines.Version, sprites.Version }.Distinct().Count());
        Assert.Equal(
            [DrawCommandKind.DrawPointBatch, DrawCommandKind.DrawLineBatch, DrawCommandKind.DrawSpriteBatch],
            commands.Select(command => command.Kind));
        Assert.Equal(points.Bounds, commands[0].Rect);
        Assert.Equal(sprites.Bounds, commands[2].Rect);
    }

    [Fact]
    public void BatchIdentityIsStableWhenReusedAndChangesForANewVersion()
    {
        DrawPointBatch first = new([new DrawPoint(1, 1)], Color.White, 2);
        DrawPointBatch replacement = new([new DrawPoint(1, 1)], Color.White, 2);

        Assert.Equal(
            DrawCommand.DrawPointBatch(first),
            DrawCommand.DrawPointBatch(first));
        Assert.NotEqual(first.Version, replacement.Version);
        Assert.NotEqual(
            DrawCommand.DrawPointBatch(first),
            DrawCommand.DrawPointBatch(replacement));
    }

    [Fact]
    public void AdvancedBoundsIncludeRotationMeshBatchesAndWorldTransform()
    {
        TestImage image = new(10, 4);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(Matrix3x2.CreateTranslation(3, 2));
        drawing.DrawImage(
            image,
            new DrawRect(10, 20, 10, 4),
            new DrawImageOptions(rotation: MathF.PI / 2));
        drawing.DrawTriangles(TriangleVertices());
        drawing.PopTransform();

        DrawCommandStateAnalysis analysis =
            new DrawCommandStateAnalyzer().Analyze(commands);

        AssertRectNear(new DrawRect(9, 22, 4, 10), analysis.Entries[1].Bounds!.Value);
        AssertRectNear(new DrawRect(4, 3, 9, 8), analysis.Entries[2].Bounds!.Value);
    }

    [Fact]
    public void FrameDelegatesEveryAdvancedFamilyTracksImagesAndEnforcesLifetime()
    {
        TestImage image = new(16, 16);
        List<IDrawImage> dependencies = [];
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 64),
            TimeSpan.Zero,
            dependencies.Add);
        DrawMesh2D mesh = new(TriangleVertices(), [0, 1, 2], image: image);
        DrawSpriteBatch sprites = new(
            image,
            [new DrawSprite2D(new DrawRect(0, 0, 8, 8))]);

        frame.DrawImage(image, new DrawRect(0, 0, 8, 8), new DrawImageOptions());
        frame.DrawImageQuad(
            image,
            new DrawPoint(0, 0),
            new DrawPoint(8, 0),
            new DrawPoint(8, 8),
            new DrawPoint(0, 8));
        frame.DrawNineSlice(image, new DrawRect(0, 0, 8, 8), new DrawInsets(2));
        frame.DrawMesh(mesh);
        frame.DrawTriangles(TriangleVertices(), image);
        frame.DrawPointBatch(new DrawPointBatch([new DrawPoint(2, 2)], Color.White));
        frame.DrawLineBatch(new DrawLineBatch(
            [new DrawLineSegment2D(new DrawPoint(0, 0), new DrawPoint(2, 2), Color.White)]));
        frame.DrawSpriteBatch(sprites);
        frame.Complete();

        Assert.Equal(8, commands.Count);
        Assert.Equal(6, dependencies.Count);
        Assert.All(dependencies, dependency => Assert.Same(image, dependency));
        Assert.Throws<ObjectDisposedException>(() =>
            frame.DrawMesh(mesh));
    }

    [Fact]
    public void PublicStageFiveContractsRemainPlatformNeutral()
    {
        Type[] stageTypes =
        [
            typeof(DrawImageOptions), typeof(DrawInsets), typeof(DrawVertex2D),
            typeof(DrawMesh2D), typeof(DrawPointBatch), typeof(DrawLineBatch),
            typeof(DrawSprite2D), typeof(DrawSpriteBatch)
        ];

        foreach (Type type in stageTypes)
        {
            IEnumerable<Type> exposed = type.GetProperties()
                .Select(property => property.PropertyType)
                .Concat(type.GetConstructors().SelectMany(constructor =>
                    constructor.GetParameters().Select(parameter => parameter.ParameterType)));
            Assert.DoesNotContain(exposed, IsMonoGameType);
        }
    }

    [Fact]
    public void OnDemandSpriteBatchTracksItsPrismImageAndOnlyRedrawsOnChange()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidImageTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        BlurFilter blur = new() { Radius = 1 };
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            blur);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.DrawSpriteBatch(new DrawSpriteBatch(
                image,
                [new DrawSprite2D(frame.Bounds)]));
        };

        _ = RenderSurface(fixture, surface);
        _ = RenderSurface(fixture, surface);
        blur.Radius = 3;
        _ = RenderSurface(fixture, surface);
        _ = RenderSurface(fixture, surface);

        Assert.Equal(2, drawCount);
    }

    [Fact]
    public void RetainedSessionSkipsReusedBatchAndDamagesOnlyChangedVersionBounds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        DrawPointBatch batch = new(
            [new DrawPoint(4, 4)],
            Color.White,
            2);

        void Draw(RenderSurface2DFrame frame) =>
            frame.DrawPointBatch(batch);

        session.Render(Draw, Color.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(16));
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        Assert.Equal(1, session.RasterizedFrameCount);
        Assert.Null(session.LastDamageBounds);

        batch = new DrawPointBatch(
            [new DrawPoint(12, 4)],
            Color.White,
            2);
        session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(32));

        Assert.Equal(2, session.RasterizedFrameCount);
        Assert.Equal(
            new Microsoft.Xna.Framework.Rectangle(3, 3, 10, 2),
            session.LastDamageBounds);
    }

    [Fact]
    public void AdvancedBatchesRenderThroughOnePrimitiveDrawEachAndRepeatDeterministically()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        Texture2D texture = new(fixture.Session.GraphicsDevice, 2, 2);
        texture.SetData(
        [
            XnaColor.Red, XnaColor.Green,
            XnaColor.Blue, XnaColor.White
        ]);
        using MonoGameImage image = new(texture);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.DrawImageQuad(
            image,
            new DrawPoint(2.25f, 2.25f),
            new DrawPoint(22.25f, 2.25f),
            new DrawPoint(22.25f, 22.25f),
            new DrawPoint(2.25f, 22.25f),
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        drawing.DrawNineSlice(
            image,
            new DrawRect(26.25f, 2.25f, 20, 20),
            new DrawInsets(1),
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        drawing.DrawPointBatch(new DrawPointBatch(
            [new DrawPoint(8, 32), new DrawPoint(16, 32)],
            Color.White,
            4));
        drawing.DrawLineBatch(new DrawLineBatch(
            [new DrawLineSegment2D(new DrawPoint(26, 32), new DrawPoint(46, 32), Color.White, 3)]));
        drawing.DrawSpriteBatch(new DrawSpriteBatch(
            image,
            [new DrawSprite2D(
                new DrawRect(52.25f, 2.25f, 20, 20),
                new DrawImageOptions(sampling: DrawSamplingMode.Point))]));

        XnaColor[] first = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);
        int firstDrawCalls = backend.LastAdvancedPrimitiveDrawCalls;
        XnaColor[] second = Render(fixture, commands);

        Assert.Equal(5, firstDrawCalls);
        Assert.Equal(5, backend.LastAdvancedPrimitiveDrawCalls);
        Assert.Equal(first, second);
        Assert.NotEqual(XnaColor.Black, Sample(first, fixture, backend, 8, 8));
        Assert.NotEqual(XnaColor.Black, Sample(first, fixture, backend, 8, 32));
        Assert.NotEqual(XnaColor.Black, Sample(first, fixture, backend, 34, 32));
    }

    [Fact]
    public void DisposedMeshImageIsRejectedWithoutCreatingOwnedGpuResources()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        Texture2D texture = new(fixture.Session.GraphicsDevice, 2, 2);
        MonoGameImage image = new(texture);
        DrawMesh2D mesh = new(TriangleVertices(), [0, 1, 2], image: image);
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawMesh(mesh);
        image.Dispose();

        Assert.Throws<ObjectDisposedException>(() => Render(fixture, commands));
    }

    private static DrawVertex2D[] TriangleVertices() =>
    [
        new DrawVertex2D(new DrawPoint(1, 1), Color.White),
        new DrawVertex2D(new DrawPoint(10, 1), Color.White),
        new DrawVertex2D(new DrawPoint(5, 9), Color.White)
    ];

    private static Texture2D CreateSolidImageTexture(
        GraphicsDevice graphicsDevice,
        XnaColor color)
    {
        Texture2D texture = new(graphicsDevice, 1, 1);
        texture.SetData([color]);
        return texture;
    }

    private static bool IsMonoGameType(Type type)
    {
        Type candidate = type.IsGenericType
            ? type.GetGenericArguments().FirstOrDefault() ?? type
            : type;
        return candidate.Namespace?.StartsWith(
            "Microsoft.Xna.Framework",
            StringComparison.Ordinal) == true;
    }

    private static XnaColor[] Render(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        DrawCommandList commands)
    {
        fixture.Session.BeginFrame(Color.Black);
        PrismFrameAnalysis prism = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext context = new(prism);
        fixture.Session.DrawingBackend.Render(commands, in context);
        fixture.Session.Present();
        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        return pixels;
    }

    private static XnaColor[] RenderSurface(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(0, 0, 96, 64),
            Color.White));
        return Render(fixture, commands);
    }

    private static XnaColor Sample(
        XnaColor[] pixels,
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        MonoGameDrawingBackend backend,
        float x,
        float y)
    {
        MonoGameDrawMapper mapper = new(backend.CoordinateScale);
        Microsoft.Xna.Framework.Rectangle sample =
            mapper.MapRectangle(new DrawRect(x, y, 1, 1));
        int width = fixture.Session.GraphicsDevice
            .PresentationParameters.BackBufferWidth;
        return pixels[(sample.Y * width) + sample.X];
    }

    private static void AssertRectNear(DrawRect expected, DrawRect actual)
    {
        Assert.InRange(MathF.Abs(actual.X - expected.X), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Y - expected.Y), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Width - expected.Width), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Height - expected.Height), 0, 0.001f);
    }

    private sealed class TestImage : IDrawImage
    {
        public TestImage(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }
    }
}

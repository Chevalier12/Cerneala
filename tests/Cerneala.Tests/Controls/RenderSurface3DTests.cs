using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface3DTests
{
    [Fact]
    public void DefaultsAndDescriptorValidationMatchTheAcceptedContract()
    {
        RenderSurface3D surface = new();
        Assert.Equal(Color.Transparent, surface.ClearColor);
        Assert.Equal(RenderSurface3DRedrawMode.OnDemand, surface.RedrawMode);
        Assert.Equal(RenderProjection3DKind.Perspective, surface.Projection.Kind);
        Assert.Equal(MathF.PI / 3, surface.Projection.VerticalFieldOfViewOrHeight, 5);
        Assert.ThrowsAny<ArgumentException>(() => surface.Projection = default);
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderProjection3D.Perspective(MathF.PI, 0.1f, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderProjection3D.Orthographic(0, 0.1f, 10));
        Assert.ThrowsAny<ArgumentException>(() => surface.ViewMatrix = default);
        Matrix4x4 nonAffine = Matrix4x4.Identity; nonAffine.M14 = 1;
        Assert.ThrowsAny<ArgumentException>(() => surface.ViewMatrix = nonAffine);
    }

    [Theory]
    [InlineData(1, 101, 51)]
    [InlineData(1.25f, 127, 64)]
    [InlineData(2, 202, 102)]
    public void RecordingCapturesOneCoherentCameraAndRasterSnapshot(float scale, int width, int height)
    {
        RenderSurface3D surface = new();
        Matrix4x4 model = Matrix4x4.CreateScale(2, 3, 4) * Matrix4x4.CreateTranslation(1, 2, 3);
        surface.Draw += (_, frame) => frame.DrawLine(Vector3.Zero, Vector3.UnitX, Color.White, model, 2);

        RenderSurface3DRecording recording = ((IRenderSurface3DSource)surface)
            .RecordFrame(new DrawRect(0, 0, 101, 51), scale);

        Assert.Equal(width, recording.PixelWidth);
        Assert.Equal(height, recording.PixelHeight);
        Assert.Equal(scale, recording.RasterScale);
        DrawPrimitive3D primitive = Assert.Single(recording.Primitives);
        Assert.Equal(model, primitive.Model);
        Vector4 actual = Vector4.Transform(new Vector4(1, 0, 0, 1), primitive.Model * recording.ViewMatrix * recording.ProjectionMatrix);
        Vector4 expected = Vector4.Transform(Vector4.Transform(Vector4.Transform(new Vector4(1, 0, 0, 1), model), recording.ViewMatrix), recording.ProjectionMatrix);
        AssertVector(expected, actual, 0.00001f);
    }

    [Fact]
    public void PerspectiveAndOrthographicProjectionRoundTripThroughRootWithNestedTransforms()
    {
        foreach (RenderProjection3D projection in new[]
        {
            RenderProjection3D.Perspective(MathF.PI / 3, 0.01f, 100),
            RenderProjection3D.Orthographic(6, 0.01f, 100)
        })
            foreach (float scale in new[] { 1f, 1.25f, 2f })
            {
                UIRoot root = new(500, 400, scale);
                UIElement parent = new() { TranslateX = 13, TranslateY = -7, ScaleX = 1.2f, ScaleY = 0.8f };
                RenderSurface3D surface = new() { Projection = projection, TranslateX = 5, TranslateY = 9 };
                root.VisualChildren.Add(parent);
                parent.VisualChildren.Add(surface);
                surface.Arrange(new ArrangeContext(new LayoutRect(37, 41, 201, 119)));
                Vector3 world = new(0.25f, -0.2f, 0);

                Assert.True(surface.TryWorldToRoot(world, out Vector2 rootPoint));
                Assert.True(surface.TryRootToWorldRay(rootPoint, out DrawRay3D ray));
                float distance = Vector3.Cross(world - ray.Origin, ray.Direction).Length();
                Assert.InRange(distance, 0, 0.0002f);
                Assert.InRange(MathF.Abs(ray.Direction.Length() - 1), 0, 0.00001f);
            }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.25f)]
    [InlineData(2)]
    public void AsymmetricCameraWorldAndRootConversionsMatchFrozenIndependentOracles(float scale)
    {
        Matrix4x4 view = Matrix4x4.CreateLookAt(
            new Vector3(3, 2, 7),
            new Vector3(0.5f, -0.25f, 0),
            Vector3.UnitY);
        UIRoot root = new(500, 400, scale);
        RenderSurface3D surface = new()
        {
            ViewMatrix = view,
            Projection = RenderProjection3D.Perspective(MathF.PI / 3, 0.01f, 1000)
        };
        root.VisualChildren.Add(surface);
        surface.Arrange(new ArrangeContext(new LayoutRect(17, 23, 320, 180)));

        AssertWorldPoint(new Vector3(0.5f, -0.25f, 0), new Vector2(177, 113));
        AssertWorldPoint(Vector3.Zero, new Vector2(167.65501f, 107.28431f));
        AssertWorldPoint(new Vector3(1.25f, 0.5f, -2), new Vector2(200.59598f, 92.63024f));
        AssertWorldPoint(new Vector3(-1, 1, 1.5f), new Vector2(131.26807f, 90.73676f));

        Assert.True(surface.TryRootToWorldRay(new Vector2(177, 113), out DrawRay3D centerRay));
        AssertVector(new Vector4(2.9967809f, 1.9971028f, 6.9909863f, 1), new Vector4(centerRay.Origin, 1), 0.00002f);
        AssertVector(new Vector4(-0.3219114f, -0.28972027f, -0.9013519f, 0), new Vector4(centerRay.Direction, 0), 0.00002f);

        void AssertWorldPoint(Vector3 world, Vector2 expected)
        {
            Assert.True(surface.TryWorldToRoot(world, out Vector2 actual));
            Assert.InRange(MathF.Abs(expected.X - actual.X), 0, 0.0001f);
            Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, 0.0001f);
        }
    }

    [Fact]
    public void ConversionRejectsOutsideViewportFrustumAndEmptyViewport()
    {
        RenderSurface3D surface = new();
        surface.Arrange(new ArrangeContext(new LayoutRect(10, 20, 100, 80)));
        Assert.False(surface.TryRootToWorldRay(new Vector2(9, 20), out _));
        Assert.False(surface.TryWorldToRoot(new Vector3(1000, 0, 0), out _));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 0, 0)));
        Assert.False(surface.TryWorldToRoot(Vector3.Zero, out _));
    }

    [Fact]
    public void SingularAndNonFiniteVisualTransformsRejectBothConversions()
    {
        (UIRoot singularRoot, UIElement singularParent, RenderSurface3D singularSurface) =
            CreateAttachedSurface();
        singularParent.ScaleX = 0;

        Assert.False(singularSurface.TryWorldToRoot(Vector3.Zero, out _));
        Assert.False(singularSurface.TryRootToWorldRay(new Vector2(50, 40), out _));

        (UIRoot nonFiniteRoot, UIElement nonFiniteParent, RenderSurface3D nonFiniteSurface) =
            CreateAttachedSurface();
        nonFiniteRoot.ScaleX = float.MaxValue;
        nonFiniteParent.ScaleX = float.MaxValue;

        Assert.False(nonFiniteSurface.TryWorldToRoot(Vector3.Zero, out _));
        Assert.False(nonFiniteSurface.TryRootToWorldRay(new Vector2(50, 40), out _));
    }

    [Fact]
    public void FinalVisualTransformOverflowReturnsFalseAndDefaultRootPosition()
    {
        (UIRoot root, UIElement parent, RenderSurface3D surface) = CreateAttachedSurface();
        parent.ScaleX = float.MaxValue;

        Assert.False(surface.TryWorldToRoot(Vector3.Zero, out Vector2 rootPosition));
        Assert.Equal(default, rootPosition);
    }

    [Fact]
    public void ExtremeFiniteOrthographicProjectionProducesAFiniteUnitRay()
    {
        RenderSurface3D surface = new()
        {
            Projection = RenderProjection3D.Orthographic(6, 0.01f, 1e30f)
        };
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.True(surface.TryRootToWorldRay(new Vector2(50, 50), out DrawRay3D ray));
        Assert.True(float.IsFinite(ray.Origin.X));
        Assert.True(float.IsFinite(ray.Origin.Y));
        Assert.True(float.IsFinite(ray.Origin.Z));
        Assert.True(float.IsFinite(ray.Direction.X));
        Assert.True(float.IsFinite(ray.Direction.Y));
        Assert.True(float.IsFinite(ray.Direction.Z));
        Assert.InRange(MathF.Abs(ray.Direction.Length() - 1), 0, 0.00001f);
        AssertVector(new Vector4(0, 0, -1, 0), new Vector4(ray.Direction, 0), 0.00001f);
    }

    [Fact]
    public void FiniteDescriptorsWhoseDerivedMatricesAreNotFiniteAreRejectedBeforePublication()
    {
        RenderSurface3D surface = new()
        {
            Projection = RenderProjection3D.Perspective(float.Epsilon, 0.01f, 100)
        };
        bool callbackInvoked = false;
        surface.Draw += (_, frame) =>
        {
            callbackInvoked = true;
            frame.DrawMarker(Vector3.Zero, Color.White);
        };
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.False(surface.TryWorldToRoot(Vector3.Zero, out _));
        Assert.False(surface.TryRootToWorldRay(new Vector2(50, 50), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 100, 100), 1));
        Assert.False(callbackInvoked);
        Assert.Throws<ArgumentException>(() => surface.ViewMatrix = Matrix4x4.CreateScale(float.Epsilon));
    }

    [Fact]
    public void WriterCopiesBatchesAndExpiresAfterComplete()
    {
        RenderSurface3D surface = new();
        RenderSurface3DFrame? captured = null;
        DrawLineSegment3D[] lines = [new(Vector3.Zero, Vector3.UnitX, Color.White, 2)];
        surface.Draw += (_, frame) => { captured = frame; frame.DrawLineBatch(lines); };
        RenderSurface3DRecording recording = ((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 20, 10), 1);
        lines[0] = new(Vector3.UnitY, Vector3.UnitZ, Color.Black, 4);

        DrawPrimitive3D primitive = Assert.Single(recording.Primitives);
        Assert.Equal(Vector3.Zero, primitive.Start);
        Assert.Equal(Vector3.UnitX, primitive.End);
        Assert.NotNull(captured);
        Assert.Throws<ObjectDisposedException>(() => captured.DrawLineBatch([]));
    }

    [Fact]
    public void EmptyBatchesAreNoOpButStillValidateWriterLifetimeFirst()
    {
        RenderSurface3D surface = new();
        RenderSurface3DFrame? frame = null;
        surface.Draw += (_, value) => { frame = value; value.DrawLineBatch([]); value.DrawMarkerBatch([]); };
        RenderSurface3DRecording recording = ((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 10, 10), 1);
        Assert.Empty(recording.Primitives);
        Assert.Throws<ObjectDisposedException>(() => frame!.DrawMarkerBatch([]));
    }

    [Fact]
    public void CallbackFailureAbortsAndExpiresWriter()
    {
        RenderSurface3D surface = new();
        RenderSurface3DFrame? frame = null;
        InvalidOperationException failure = new("boom");
        surface.Draw += (_, value) => { frame = value; throw failure; };
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            ((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 10, 10), 1)));
        Assert.Throws<ObjectDisposedException>(() => frame!.DrawMarker(Vector3.Zero, Color.White));
    }

    [Fact]
    public void PrimitiveAndModelValidationRejectsInvalidInputButAllowsSingularAffineModel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawMarker3D(Vector3.Zero, new Color(1, 2, 3, 254)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawLineSegment3D(Vector3.Zero, Vector3.One, Color.White, float.NaN));
        RenderSurface3D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawMarker(Vector3.Zero, Color.White, Matrix4x4.CreateScale(0));
            Matrix4x4 invalid = Matrix4x4.Identity; invalid.M14 = 1;
            Assert.Throws<ArgumentOutOfRangeException>(() => frame.DrawMarker(Vector3.Zero, Color.White, invalid));
        };
        Assert.Single(((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 10, 10), 1).Primitives);
        Assert.Throws<OverflowException>(() => RenderSurface3DFrame.ValidateBatchSize<DrawLineSegment3D>(int.MaxValue));
    }

    [Fact]
    public void OnDemandContinuousSubscriptionsAndCallbackInvalidationPreserveNextGeneration()
    {
        RenderSurface3D surface = new();
        Assert.False(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(1)));
        int calls = 0;
        surface.Draw += (_, _) => { calls++; if (calls == 1) surface.InvalidateFrame(); };
        IRenderSurface3DSource source = surface;
        long requested = source.FrameVersion;
        RenderSurface3DRecording first = source.RecordFrame(new DrawRect(0, 0, 10, 10), 1);
        Assert.Equal(requested, first.Generation);
        Assert.True(source.FrameVersion > first.Generation);
        Assert.Equal(1, calls);
        Assert.False(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(2)));
        surface.RedrawMode = RenderSurface3DRedrawMode.Continuous;
        Assert.True(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(3)));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SubscriptionRemovalAndMutationDuringDispatchPreserveTheNextFrame()
    {
        RenderSurface3D surface = new();
        IRenderSurface3DSource source = surface;
        List<string> calls = [];
        bool mutated = false;
        RenderSurface3DDrawEventHandler third = (_, _) => calls.Add("third");
        RenderSurface3DDrawEventHandler second = (_, _) => calls.Add("second");
        RenderSurface3DDrawEventHandler? first = null;
        first = (sender, _) =>
        {
            calls.Add("first");
            if (mutated) return;
            mutated = true;
            sender.Draw -= second;
            sender.Draw += third;
        };
        surface.Draw += first;
        surface.Draw += second;
        long firstRequestedGeneration = source.FrameVersion;

        RenderSurface3DRecording firstRecording = source.RecordFrame(new DrawRect(0, 0, 10, 10), 1);

        Assert.Equal(["first", "second"], calls);
        Assert.Equal(firstRequestedGeneration, firstRecording.Generation);
        Assert.True(source.FrameVersion > firstRecording.Generation);
        long nextRequestedGeneration = source.FrameVersion;
        calls.Clear();

        RenderSurface3DRecording nextRecording = source.RecordFrame(new DrawRect(0, 0, 10, 10), 1);

        Assert.Equal(["first", "third"], calls);
        Assert.Equal(nextRequestedGeneration, nextRecording.Generation);
        surface.Draw -= first;
        surface.Draw -= third;

        UIRoot root = new(20, 20);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.DoesNotContain(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.RenderSurface3D);
    }

    [Fact]
    public void CallbackCameraAndClearMutationsDoNotTearTheRecordingSnapshot()
    {
        Color requestedClear = new(10, 20, 30, 40);
        Matrix4x4 requestedView = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        RenderSurface3D surface = new() { ClearColor = requestedClear, ViewMatrix = requestedView };
        surface.Draw += (sender, frame) =>
        {
            Assert.Equal(requestedView, frame.ViewMatrix);
            sender.ClearColor = new Color(50, 60, 70, 80);
            sender.ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(2, 0, 5), Vector3.Zero, Vector3.UnitY);
            frame.DrawMarker(Vector3.Zero, Color.White);
        };

        RenderSurface3DRecording recording = ((IRenderSurface3DSource)surface)
            .RecordFrame(new DrawRect(0, 0, 20, 10), 1);

        Assert.Equal(requestedClear, recording.ClearColor);
        Assert.Equal(requestedView, recording.ViewMatrix);
        Assert.True(((IRenderSurface3DSource)surface).FrameVersion > recording.Generation);
    }

    [Fact]
    public void CameraMutationChangesContentGenerationButUiOpacityAndPositionDoNot()
    {
        RenderSurface3D surface = new();
        surface.Draw += (_, _) => { };
        IRenderSurface3DSource source = surface;
        long initial = source.FrameVersion;
        surface.Opacity = 0.5f;
        surface.TranslateX = 10;
        Assert.Equal(initial, source.FrameVersion);
        surface.ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(1, 0, 5), Vector3.Zero, Vector3.UnitY);
        Assert.True(source.FrameVersion > initial);
    }

    [Fact]
    public void CameraMutationDoesNotScheduleOrExecuteMeasureOrArrange()
    {
        UIRoot root = new(200, 100);
        CountingRenderSurface3D surface = new();
        surface.Draw += static (_, _) => { };
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        int measureCount = surface.MeasureCount;
        int arrangeCount = surface.ArrangeCount;

        surface.ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(1, 0, 5), Vector3.Zero, Vector3.UnitY);
        root.ProcessFrame();

        Assert.Equal(measureCount, surface.MeasureCount);
        Assert.Equal(arrangeCount, surface.ArrangeCount);
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(0, root.LayoutQueue.ArrangeCount);
    }

    [Fact]
    public void ParentPrismKeyTracksCameraButThreeDimensionalContentIdentityIgnoresUiTransformAndOpacity()
    {
        UIRoot root = new(200, 100);
        ContentControl parent = new();
        CountingRenderSurface3D surface = new();
        surface.Draw += static (_, frame) => frame.DrawMarker(Vector3.Zero, Color.White);
        parent.Content = surface;
        using IDisposable lease = PrismAttachment.Set(
            parent,
            () => new PrismInstance(PrismTestData.Composition(
                "RenderSurface3D parent",
                PrismTestData.Layer(1, "Content"))),
            []);
        root.VisualChildren.Add(parent);

        RenderPipelineSnapshot initial = CaptureRenderPipeline(root);
        surface.ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(1, 0, 5), Vector3.Zero, Vector3.UnitY);
        RenderPipelineSnapshot cameraChanged = CaptureRenderPipeline(root);

        Assert.Same(initial.Source, cameraChanged.Source);
        Assert.NotEqual(initial.Generation, cameraChanged.Generation);
        Assert.NotEqual(initial.PrismDependencyStamp, cameraChanged.PrismDependencyStamp);
        Assert.NotEqual(initial.PrismKey, cameraChanged.PrismKey);

        surface.TranslateX = 7;
        surface.Opacity = 0.5f;
        RenderPipelineSnapshot uiChanged = CaptureRenderPipeline(root);

        Assert.Same(cameraChanged.Source, uiChanged.Source);
        Assert.Equal(cameraChanged.Generation, uiChanged.Generation);
    }

    [Fact]
    public void SurfaceCommandCarriesSourceGenerationAndTransformsWithoutLosingIdentity()
    {
        RenderSurface3D surface = new();
        surface.Draw += (_, _) => { };
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 30)));
        RetainedRenderCache cache = new();
        cache.GetElementCache(surface).Ensure(surface, new RenderCounters(), forceRebuild: true);
        new DrawCommandListBuilder().Build(surface, cache, new RenderCounters());
        DrawCommand command = Assert.Single(cache.RootCommands);
        Assert.Equal(DrawCommandKind.RenderSurface3D, command.Kind);
        Assert.Same(surface, command.RenderSurface3D);
        Assert.Equal(((IRenderSurface3DSource)surface).FrameVersion, command.RetainedVersion);
        DrawCommand translated = DrawCommandTransform.Translate(command, 5, 7);
        DrawCommand faded = DrawCommandTransform.ApplyOpacity(translated, 0.5f);
        Assert.Same(command.RenderSurface3D, faded.RenderSurface3D);
        Assert.Equal(command.RetainedVersion, faded.RetainedVersion);
    }

    private static void AssertVector(Vector4 expected, Vector4 actual, float tolerance)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.Z - actual.Z), 0, tolerance);
        Assert.InRange(MathF.Abs(expected.W - actual.W), 0, tolerance);
    }

    private static (UIRoot Root, UIElement Parent, RenderSurface3D Surface) CreateAttachedSurface()
    {
        UIRoot root = new(200, 160);
        UIElement parent = new();
        RenderSurface3D surface = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(surface);
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));
        return (root, parent, surface);
    }

    private static RenderPipelineSnapshot CaptureRenderPipeline(UIRoot root)
    {
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        DrawCommand command = Assert.Single(commands.Where(candidate => candidate.Kind == DrawCommandKind.RenderSurface3D));
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        PrismAnalyzedScope analyzedScope = Assert.Single(analysis.Scopes);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(analysis));
        PrismGraphScope graphScope = Assert.Single(plan.OptimizedGraph.Scopes);
        PrismGraphNodeId output = Assert.IsType<PrismGraphNodeId>(graphScope.Output);
        PrismRetainedRasterContext rasterContext = new(
            200,
            100,
            PrismColorProfile.LinearSrgb,
            BackdropPixelFormat.Rgba8Unorm,
            PrismSampling.Linear,
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.FilterProcessing |
            PrismGraphCapabilities.StyleProcessing |
            PrismGraphCapabilities.MaskProcessing |
            PrismGraphCapabilities.GroupProcessing |
            PrismGraphCapabilities.GroupIsolation |
            PrismGraphCapabilities.Clipping |
            PrismGraphCapabilities.AdvancedBlending |
            PrismGraphCapabilities.ColorConversion,
            shaderPackageVersion: 1);
        Assert.True(PrismRetainedCacheKey.TryCreate(plan, output, rasterContext, out PrismRetainedCacheKey key));
        return new(command.RenderSurface3D!, command.RetainedVersion, analyzedScope.DependencyStamp, key);
    }

    private sealed class CountingRenderSurface3D : RenderSurface3D
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new LayoutSize(100, 80);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return base.ArrangeCore(context);
        }
    }

    private readonly record struct RenderPipelineSnapshot(
        IRenderSurface3DSource Source,
        long Generation,
        PrismDependencyStamp PrismDependencyStamp,
        PrismRetainedCacheKey PrismKey);
}

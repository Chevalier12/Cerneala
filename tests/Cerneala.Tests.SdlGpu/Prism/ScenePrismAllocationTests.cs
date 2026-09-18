using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Surfaces;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.SdlGpu.Prism;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

[Collection(SdlNativeTestCollection.Name)]
public sealed class ScenePrismAllocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocalScopeTranslationAndTransformPreserveAllocationPolicy(bool strict)
    {
        PrismInstance instance = new(new PrismCompositionDefinition("Local scope",
            [new PrismLayerDefinition(new(1), "Invert", filters: [new(PrismFilterId.Invert)])]));
        PrismDrawScope scope = new(instance, new(98001), new(0, 0, 48, 32),
            Matrix3x2.Identity, 1, 1, PrismDrawResources.Empty, isLocalDrawingScope: true)
        {
            StrictSurfaceAllocation = strict,
            InputBounds = new(2, 3, 12, 8)
        };

        PrismDrawScope translated = scope.TranslateLocal(11, -7);
        PrismDrawScope transformed = translated.ApplyLocalTransform(Matrix3x2.CreateScale(2));

        Assert.Equal(new DrawRect(11, -7, 48, 32), translated.ControlBounds);
        Assert.Equal(Matrix3x2.CreateScale(2), transformed.EffectiveTransform);
        Assert.Equal(new DrawRect(13, -4, 12, 8), translated.InputBounds);
        Assert.Equal(translated.InputBounds, transformed.InputBounds);
        Assert.Equal(strict, translated.StrictSurfaceAllocation);
        Assert.Equal(strict, transformed.StrictSurfaceAllocation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StreamedSceneRejectsAllocationFailureInsteadOfDrawingWithoutItsEffect(bool nested)
    {
        using SceneFixture scene = new(streamed: true, nested);
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("streamed-prism-allocation", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 48, 32, 1));
        DrawCommandList commands = scene.Record();
        Assert.Equal(nested ? 2 : 1, commands.Count(command => command.Kind == DrawCommandKind.BeginPrism));
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.FillRectangle);
        PrismSurfaceAllocationException failure = Assert.Throws<PrismSurfaceAllocationException>(
            () => Render(session, commands, () => api.FailTextureCreationAt = api.TextureCreationCount + 1));

        Assert.True(failure.RequestedByteCount > 0);
        Assert.Equal(new PrismRendererOptions().SurfaceHardByteLimit, failure.HardByteLimit);
        Assert.Equal(0, Diagnostics(session).Counters.FallbackCount);
        Assert.Equal(0, Diagnostics(session).Counters.PassCount);

        // The failure is not cached as a successful, unfiltered composition.
        // A later explicit frame can recover after allocation becomes possible.
        api.FailTextureCreationAt = 0;
        Render(session, commands);
        Assert.Equal(0, Diagnostics(session).Counters.FallbackCount);
        Assert.True(Diagnostics(session).Counters.CaptureCount > 0);
        session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(session.DrawingResources.PrismResources.TotalBytes,
            session.DrawingResources.PrismResources.FreeBytes);
    }

    [Fact]
    public void SceneWithoutSpatialSourcesKeepsItsExistingAllocationFallback()
    {
        using SceneFixture scene = new(streamed: false, nested: false);
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("ordinary-prism-allocation", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 48, 32, 1));
        Render(session, scene.Record(), () => api.FailTextureCreationAt = api.TextureCreationCount + 1);

        Assert.Equal(PrismFallbackReason.SurfaceAllocationFailed, Diagnostics(session).LastFallback?.Reason);
    }

    [Fact]
    public void DeclaredDomainOverHardMemoryLimitFailsBeforeTextureCreation()
    {
        using SceneFixture scene = new(streamed: true, nested: false, domain: new(0, 0, 65536, 65536));
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-domain-budget", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 48, 32, 1));
        int createdBeforeExecution = -1;

        PrismSurfaceAllocationException failure = Assert.Throws<PrismSurfaceAllocationException>(() =>
            Render(session, scene.Record(), () => createdBeforeExecution = api.TextureCreationCount));

        Assert.True(failure.RequestedByteCount > failure.HardByteLimit);
        Assert.Equal(createdBeforeExecution, api.TextureCreationCount);
        Assert.Equal(0, Diagnostics(session).Counters.FallbackCount);
    }

    [Fact]
    public void DeclaredDomainWithUnrepresentableRasterExtentFailsExplicitly()
    {
        // The logical rectangle is valid, but at 2x density its raster width
        // exceeds Int32. Invalid DrawRect construction is not a renderer test.
        using SceneFixture scene = new(streamed: true, nested: false, domain: new(0, 0, 1_500_000_000, 32));
        FakeSdlApi api = new() { WindowPixelDensity = 2 };
        nint window = api.CreateWindow("prism-domain-raster", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 48, 32, 2));

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => Render(session, scene.Record()));
        Assert.Contains("representable raster", failure.Message);
        Assert.Equal(0, Diagnostics(session).Counters.FallbackCount);
    }

    private static void Render(SdlGpuWindowGraphicsSession session, DrawCommandList commands,
        Action? beforeExecute = null)
    {
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        session.BeginFrame(Color.Transparent);
        try
        {
            beforeExecute?.Invoke();
            session.DrawingBackend.Render(commands, in frame);
        }
        finally { session.CompleteFrame(present: false); }
    }

    private static PrismExecutionDiagnostics Diagnostics(SdlGpuWindowGraphicsSession session) =>
        Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend).PrismDiagnostics;

    private sealed class SceneFixture : IDisposable
    {
        private readonly UIRoot root = new(48, 32);
        private readonly RenderSurface2D surface = new() { ViewBox = new(0, 0, 48, 32) };
        private readonly List<IDisposable> effects = [];

        internal SceneFixture(bool streamed, bool nested, DrawRect? domain = null)
        {
            Scene2D scene = new() { PrismInputDomain = domain };
            Scene2D owner = scene;
            if (nested)
            {
                owner = new();
                scene.Children.Add(owner);
            }
            if (streamed)
            {
                owner.Children.Add(new SceneItems2D
                {
                    ItemsSource = new SceneSpatialSource2D<object>([new("shape", new(0, 0, 48, 32))],
                        (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(new SolidNode())))
                });
            }
            else { owner.Children.Add(new SolidNode()); }
            effects.Add(Attach(scene, domain is null ? PrismFilterId.Invert : PrismFilterId.Threshold));
            if (nested) { effects.Add(Attach(owner)); }
            surface.Scene = scene;
            root.VisualChildren.Add(surface);
            root.ProcessFrame();
        }

        internal DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new(0, 0, 48, 32));
            Assert.Equal(RenderSurface2DPresentationState.Ready, surface.PresentationState);
            return commands;
        }

        private static IDisposable Attach(Scene2D owner, PrismFilterId filter = PrismFilterId.Invert) => GeneratedMarkup.AttachPrism(owner,
            () => new PrismInstance(new PrismCompositionDefinition("Scene allocation contract",
                [new PrismLayerDefinition(new(1), "Filter", filters: [new(filter)])])));

        public void Dispose()
        {
            root.VisualChildren.Remove(surface);
            foreach (IDisposable effect in effects) { effect.Dispose(); }
        }
    }

    private sealed class SolidNode : SceneNode2D
    {
        internal override void Record(Scene2DRecordContext context) =>
            context.Frame.FillRectangle(new(0, 0, 48, 32), Color.Coral);

        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Known(new(0, 0, 48, 32));
    }
}

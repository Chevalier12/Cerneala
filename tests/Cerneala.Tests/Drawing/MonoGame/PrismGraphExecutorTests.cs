using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismGraphExecutorTests
{
    private const int SurfaceWidth = 16;
    private const int SurfaceHeight = 16;
    private const int MeasuredFrameCount = 16;

    [Fact]
    public void RegistryValidatesCatalogAndRegistersOnlyFundamentalKernels()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "blend-mode",
                "Normal"));
        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "blend-mode",
                "PassThrough"));
        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "color-profile",
                "LinearSrgb"));
        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "color-profile",
                "Srgb"));
        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "sampling",
                "Linear"));
        Assert.False(
            registry.IsFundamentalCatalogEntryRegistered(
                "filter",
                "Blur"));
        Assert.False(
            registry.IsFundamentalCatalogEntryRegistered(
                "style",
                "DropShadow"));

        Assert.True(
            registry.TryGetColorConversionKernel(
                PrismColorProfile.LinearSrgb,
                out PrismKernel toLinear));
        Assert.Equal(PrismKernelKind.SrgbToLinear, toLinear.Kind);
        Assert.True(
            registry.TryGetColorConversionKernel(
                PrismColorProfile.Srgb,
                out PrismKernel toSrgb));
        Assert.Equal(PrismKernelKind.LinearToSrgb, toSrgb.Kind);
    }

    [Fact]
    public void SimpleCompositionCapturesOnceAndAllocatesNothingAfterWarmup()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        (
            DrawCommandList commands,
            PrismFrameAnalysis analysis,
            PrismGraphExecutionPlan plan) =
            CreateSimpleComposition();
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);

        for (int frame = 0; frame < 8; frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                commands,
                analysis,
                plan,
                viewport);
        }

        long createdAfterWarmup =
            executor.SurfacePool.CreatedSurfaceCount;
        long reusedAfterWarmup =
            executor.SurfacePool.ReusedSurfaceCount;
        renderer.ResetRenderedCommandCount();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        ExecuteFrame(
            renderer,
            executor,
            commands,
            analysis,
            plan,
            viewport);
        renderer.ResetRenderedCommandCount();

        long allocationStart =
            GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = 0;
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            renderer.BeginFrame();
            try
            {
                executor.Execute(
                    commands,
                    analysis,
                    plan,
                    renderer,
                    viewport,
                    backdropLease: null);
            }
            finally
            {
                renderer.EndBatch();
            }
        }
        allocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() -
            allocationStart;

        Assert.Equal(0, allocatedBytes);
        Assert.Equal(
            MeasuredFrameCount,
            renderer.RenderedCommandCount);
        Assert.Equal(
            createdAfterWarmup,
            executor.SurfacePool.CreatedSurfaceCount);
        Assert.True(
            executor.SurfacePool.ReusedSurfaceCount >
            reusedAfterWarmup);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
        Assert.Equal(1, diagnostics.Count);
        Assert.Equal(
            PrismFallbackReason.MissingKernel,
            diagnostics.Get(0).Reason);
        Assert.Equal(
            PrismFallbackAction.BypassOperation,
            diagnostics.Get(0).Action);

        XnaColor pixel = renderer.ReadCenterPixel();
        Assert.InRange(pixel.R, 126, 129);
        Assert.InRange(pixel.G, 126, 129);
        Assert.InRange(pixel.B, 126, 129);
        Assert.InRange(pixel.A, 126, 129);
    }

    [Fact]
    public void BackendRoutesPrismFramesThroughTheExecutor()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        MonoGameDrawingBackend backend =
            Assert.IsType<MonoGameDrawingBackend>(
                fixture.Session.DrawingBackend);
        (DrawCommandList commands, _, _) =
            CreateSimpleComposition(96, 64);
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);

        fixture.Session.BeginFrame(CernealaColor.Transparent);
        backend.Render(commands, in frameContext);
        fixture.Session.Present();

        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[
                parameters.BackBufferWidth *
                parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        XnaColor pixel = pixels[
            ((parameters.BackBufferHeight / 2) *
                parameters.BackBufferWidth) +
            (parameters.BackBufferWidth / 2)];

        Assert.InRange(pixel.R, 126, 129);
        Assert.InRange(pixel.G, 126, 129);
        Assert.InRange(pixel.B, 126, 129);
        Assert.InRange(pixel.A, 126, 129);
        Assert.Equal(1, backend.PrismDiagnostics.Count);
        Assert.Equal(
            PrismFallbackReason.MissingKernel,
            backend.PrismDiagnostics.Get(0).Reason);
    }

    private static (
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan)
        CreateSimpleComposition(
            int width = SurfaceWidth,
            int height = SurfaceHeight)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Executor gate",
                PrismTestData.Layer(
                    1,
                    "Half opacity",
                    opacity: 0.5f)),
            bounds: new DrawRect(0, 0, width, height));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, width, height),
                CernealaColor.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraph graph =
            new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        return (commands, analysis, plan);
    }

    private static void ExecuteFrame(
        TestPrismRenderer renderer,
        PrismGraphExecutor executor,
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        Viewport viewport)
    {
        renderer.BeginFrame();
        try
        {
            executor.Execute(
                commands,
                analysis,
                plan,
                renderer,
                viewport,
                backdropLease: null);
        }
        finally
        {
            renderer.EndBatch();
        }
    }

    private sealed class TestPrismRenderer :
        IPrismCommandRenderer,
        IDisposable
    {
        private readonly SpriteBatch spriteBatch;
        private readonly Texture2D whitePixel;
        private readonly RenderTarget2D hostTarget;
        private bool batchActive;

        public TestPrismRenderer(
            GraphicsDevice graphicsDevice,
            int width,
            int height)
        {
            GraphicsDevice = graphicsDevice;
            spriteBatch = new SpriteBatch(graphicsDevice);
            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([XnaColor.White]);
            hostTarget = new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.Color,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int RenderedCommandCount { get; private set; }

        public void BeginFrame()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(hostTarget);
            GraphicsDevice.Clear(XnaColor.Transparent);
            BeginCommandBatch();
        }

        public void ResetRenderedCommandCount()
        {
            RenderedCommandCount = 0;
        }

        public XnaColor ReadCenterPixel()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(null);
            XnaColor[] pixels =
                new XnaColor[hostTarget.Width * hostTarget.Height];
            hostTarget.GetData(pixels);
            return pixels[
                ((hostTarget.Height / 2) * hostTarget.Width) +
                (hostTarget.Width / 2)];
        }

        public void BeginCommandBatch()
        {
            BeginBatch(effect: null, BlendState.AlphaBlend);
        }

        public void BeginKernelBatch(
            Effect effect,
            BlendState blendState)
        {
            BeginBatch(effect, blendState);
        }

        public void EndBatch()
        {
            if (!batchActive)
            {
                return;
            }

            try
            {
                spriteBatch.End();
            }
            finally
            {
                batchActive = false;
            }
        }

        public void RenderCommand(DrawCommand command)
        {
            if (command.Kind != DrawCommandKind.FillRectangle ||
                command.Brush is not null)
            {
                throw new InvalidOperationException(
                    $"Unsupported executor test command '{command.Kind}'.");
            }

            RenderedCommandCount++;
            Rectangle destination = new(
                (int)MathF.Round(command.Rect.X),
                (int)MathF.Round(command.Rect.Y),
                (int)MathF.Round(command.Rect.Width),
                (int)MathF.Round(command.Rect.Height));
            spriteBatch.Draw(
                whitePixel,
                destination,
                new XnaColor(
                    command.Color.R,
                    command.Color.G,
                    command.Color.B,
                    command.Color.A));
        }

        public void DrawFullscreen(
            Texture2D texture,
            Rectangle destination)
        {
            spriteBatch.Draw(
                texture,
                destination,
                XnaColor.White);
        }

        public void RestoreHostTarget()
        {
            GraphicsDevice.SetRenderTarget(hostTarget);
            GraphicsDevice.Viewport =
                new Viewport(
                    0,
                    0,
                    hostTarget.Width,
                    hostTarget.Height);
        }

        public void Dispose()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(null);
            hostTarget.Dispose();
            whitePixel.Dispose();
            spriteBatch.Dispose();
        }

        private void BeginBatch(
            Effect? effect,
            BlendState blendState)
        {
            if (batchActive)
            {
                throw new InvalidOperationException(
                    "The executor test SpriteBatch is already active.");
            }

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                blendState,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect);
            batchActive = true;
        }
    }

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala Prism executor {Guid.NewGuid():N}",
                    Width = 96,
                    Height = 64
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session =
                Assert.IsType<WindowsDxWindowGraphicsSession>(
                    window.GraphicsSession);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested() { }
    }
}

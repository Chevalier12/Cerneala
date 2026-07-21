using System.Diagnostics;
using System.Globalization;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Benchmarks;

internal static class PrismRetainedCacheBenchmarkRunner
{
    private const int SurfaceWidth = 256;
    private const int SurfaceHeight = 144;
    private const int WarmupFrameCount = 12;
    private const int MeasuredFrameCount = 96;
    private const int CompletionFrameCount = 8;
    private const int DynamicFrameCount =
        WarmupFrameCount + MeasuredFrameCount +
        CompletionFrameCount + 1;
    private const int CommonInstanceCount = 24;

    public static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The Prism retained-cache benchmark requires WindowsDX.");
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"PRISM_RETAINED_HARDWARE " +
                $"adapter=\"{graphicsDevice.Adapter.Description}\" " +
                $"profile={graphicsDevice.GraphicsProfile} " +
                $"processors={Environment.ProcessorCount} " +
                $"os=\"{Environment.OSVersion.VersionString}\""));

        foreach (BenchmarkScenarioKind kind in
            Enum.GetValues<BenchmarkScenarioKind>())
        {
            using BenchmarkScenario scenario =
                CreateScenario(graphicsDevice, kind);
            RunScenario(
                graphicsDevice,
                scenario,
                retainedCacheEnabled: false);
            RunScenario(
                graphicsDevice,
                scenario,
                retainedCacheEnabled: true);
        }
    }

    private static void RunScenario(
        GraphicsDevice graphicsDevice,
        BenchmarkScenario scenario,
        bool retainedCacheEnabled)
    {
        using BenchmarkRenderer renderer = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics executionDiagnostics = new();
        using PrismGraphExecutor executor = new(
            graphicsDevice,
            executionDiagnostics,
            scenario.Options,
            retainedCacheEnabled);
        Viewport viewport = new(
            0,
            0,
            SurfaceWidth,
            SurfaceHeight);

        for (int frame = 0;
            frame < WarmupFrameCount;
            frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                scenario.GetFrame(frame),
                viewport);
        }
        renderer.Synchronize();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        ExecuteFrame(
            renderer,
            executor,
            scenario.GetFrame(WarmupFrameCount),
            viewport);
        renderer.Synchronize();
        PrismRendererDiagnostics before =
            executor.RendererDiagnostics;
        long allocationStart =
            GC.GetAllocatedBytesForCurrentThread();
        long cpuStarted = Stopwatch.GetTimestamp();
        for (int frame = 0;
            frame < MeasuredFrameCount;
            frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                scenario.GetFrame(
                    WarmupFrameCount + frame + 1),
                viewport);
        }
        TimeSpan cpuElapsed =
            Stopwatch.GetElapsedTime(cpuStarted);
        long allocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() -
            allocationStart;
        PrismRendererDiagnostics after =
            executor.RendererDiagnostics;

        long completionStarted = Stopwatch.GetTimestamp();
        for (int frame = 0;
            frame < CompletionFrameCount;
            frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                scenario.GetFrame(
                    WarmupFrameCount +
                    MeasuredFrameCount +
                    frame + 1),
                viewport);
            renderer.Synchronize();
        }
        TimeSpan completionUpperBound =
            Stopwatch.GetElapsedTime(completionStarted);

        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"PRISM_RETAINED_BENCHMARK " +
                $"scenario={scenario.Name} " +
                $"cache={(retainedCacheEnabled ? "on" : "off")} " +
                $"frames={MeasuredFrameCount} " +
                $"cpu-submit-us=" +
                $"{cpuElapsed.TotalMicroseconds / MeasuredFrameCount:F3} " +
                $"gpu-completion-upper-bound-us=" +
                $"{completionUpperBound.TotalMicroseconds / CompletionFrameCount:F3} " +
                $"allocated-bytes={allocatedBytes} " +
                $"final-hits=" +
                $"{after.FinalHitCount - before.FinalHitCount} " +
                $"intermediate-hits=" +
                $"{after.IntermediateHitCount - before.IntermediateHitCount} " +
                $"misses={after.MissCount - before.MissCount} " +
                $"lookups={after.LookupCount - before.LookupCount} " +
                $"promotions=" +
                $"{after.PromotionCount - before.PromotionCount} " +
                $"rejected-promotions=" +
                $"{after.RejectedPromotionCount - before.RejectedPromotionCount} " +
                $"evictions=" +
                $"{after.EvictionCount - before.EvictionCount} " +
                $"capacity-evictions=" +
                $"{after.GetEvictionCount(PrismCacheEvictionReason.Capacity) - before.GetEvictionCount(PrismCacheEvictionReason.Capacity)} " +
                $"replacement-evictions=" +
                $"{after.GetEvictionCount(PrismCacheEvictionReason.Replacement) - before.GetEvictionCount(PrismCacheEvictionReason.Replacement)} " +
                $"transient-pressure-evictions=" +
                $"{after.GetEvictionCount(PrismCacheEvictionReason.TransientPressure) - before.GetEvictionCount(PrismCacheEvictionReason.TransientPressure)} " +
                $"retained-entries={after.RetainedEntryCount} " +
                $"pinned-entries={after.PinnedEntryCount} " +
                $"retained-bytes={after.RetainedByteCount} " +
                $"peak-total-bytes={after.PeakTotalByteCount} " +
                $"saved-captures=" +
                $"{after.SavedCaptureCount - before.SavedCaptureCount} " +
                $"saved-passes=" +
                $"{after.SavedPassCount - before.SavedPassCount}"));
    }

    private static void ExecuteFrame(
        BenchmarkRenderer renderer,
        PrismGraphExecutor executor,
        BenchmarkFrame frame,
        Viewport viewport)
    {
        renderer.BeginFrame();
        try
        {
            executor.Execute(
                frame.Commands,
                frame.Analysis,
                frame.Plan,
                renderer,
                viewport,
                frame.BackdropLease);
        }
        finally
        {
            renderer.EndBatch();
        }
    }

    private static BenchmarkScenario CreateScenario(
        GraphicsDevice graphicsDevice,
        BenchmarkScenarioKind kind) =>
        kind switch
        {
            BenchmarkScenarioKind.StaticControl =>
                CreateStaticControlScenario(),
            BenchmarkScenarioKind.StaticBackdrop =>
                CreateBackdropScenario(
                    graphicsDevice,
                    animated: false),
            BenchmarkScenarioKind.AnimatedGameBackdrop =>
                CreateBackdropScenario(
                    graphicsDevice,
                    animated: true),
            BenchmarkScenarioKind.MotionParameter =>
                CreateMotionParameterScenario(),
            BenchmarkScenarioKind.ChangedResource =>
                CreateChangedResourceScenario(graphicsDevice),
            BenchmarkScenarioKind.ManyCommonInstances =>
                CreateManyCommonInstancesScenario(),
            BenchmarkScenarioKind.SmallBudget =>
                CreateSmallBudgetScenario(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown Prism benchmark scenario.")
        };

    private static BenchmarkScenario
        CreateStaticControlScenario()
    {
        PrismCompositionDefinition definition =
            CreateFilteredControlDefinition("Static control");
        PrismInstance instance = new(definition);
        PrismDrawScope scope = CreateScope(
            instance,
            ownerToken: 1_001,
            visualContentVersion: 1);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(69, 145, 232, 224));
        return new BenchmarkScenario(
            "static-control",
            [frame],
            new PrismRendererOptions());
    }

    private static BenchmarkScenario CreateBackdropScenario(
        GraphicsDevice graphicsDevice,
        bool animated)
    {
        Texture2D texture = CreateBackdropTexture(graphicsDevice);
        PrismCompositionDefinition definition = new(
            animated
                ? "Animated game backdrop"
                : "Static backdrop",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Foreground",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.Invert)
                    ],
                    opacity: 0.72f),
                new PrismBackdropDefinition(
                    new PrismNodeId(2),
                    "Host backdrop",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur)
                    ])
            ]);
        PrismInstance instance = new(definition);
        PrismDrawScope scope = CreateScope(
            instance,
            ownerToken: animated ? 3_001 : 2_001,
            visualContentVersion: 1,
            lowerUiVersion: 1);
        DrawCommandList commands = CreateCommands(
            scope,
            new CernealaColor(232, 78, 102, 192));
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismBackdropSourceToken sourceToken =
            PrismBackdropSourceToken.CreateUnique();
        int frameCount = animated ? DynamicFrameCount : 1;
        BenchmarkFrame[] frames =
            new BenchmarkFrame[frameCount];
        for (int index = 0; index < frameCount; index++)
        {
            BackdropFrameMetadata metadata = new(
                SurfaceWidth,
                SurfaceHeight,
                1,
                PrismColorProfile.Srgb,
                BackdropPixelFormat.Rgba8Unorm,
                BackdropAlphaMode.Opaque,
                System.Numerics.Matrix3x2.Identity,
                animated ? 10_000 + index : 10_000);
            PrismGraphExecutionPlan plan =
                new PrismGraphOptimizer().Optimize(
                    new PrismGraphBuilder().Build(
                        analysis,
                        metadata,
                        sourceToken));
            frames[index] = new BenchmarkFrame(
                commands,
                analysis,
                plan,
                new BorrowedBackdropLease(
                    texture,
                    metadata));
        }

        return new BenchmarkScenario(
            animated
                ? "animated-game-backdrop"
                : "static-backdrop",
            frames,
            new PrismRendererOptions(),
            texture);
    }

    private static BenchmarkScenario
        CreateMotionParameterScenario()
    {
        PrismCompositionDefinition definition =
            CreateFilteredControlDefinition("Motion parameter");
        BenchmarkFrame[] frames =
            new BenchmarkFrame[DynamicFrameCount];
        for (int index = 0;
            index < frames.Length;
            index++)
        {
            PrismInstance instance = new(definition);
            float opacity = 0.2f +
                (0.75f * index / (frames.Length - 1));
            instance.GetLayerState(
                new PrismNodeId(1)).Opacity = opacity;
            PrismDrawScope scope = CreateScope(
                instance,
                ownerToken: 4_001,
                visualContentVersion: index + 1);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(82, 204, 142, 224));
        }

        return new BenchmarkScenario(
            "motion-parameter",
            frames,
            new PrismRendererOptions());
    }

    private static BenchmarkScenario
        CreateChangedResourceScenario(
            GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight,
            false,
            SurfaceFormat.Color);
        XnaColor[] maskPixels =
            new XnaColor[SurfaceWidth * SurfaceHeight];
        for (int y = 0; y < SurfaceHeight; y++)
        {
            for (int x = 0; x < SurfaceWidth; x++)
            {
                byte alpha = (byte)(48 +
                    ((x + y) % 176));
                maskPixels[(y * SurfaceWidth) + x] =
                    new XnaColor(alpha, alpha, alpha, alpha);
            }
        }
        texture.SetData(maskPixels);
        MonoGameImage image = new(texture);
        PrismResourceId maskId = new("BenchmarkMask");
        PrismCompositionDefinition definition = new(
            "Changed resource",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Masked content",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur)
                    ],
                    mask: new PrismMaskDefinition(
                        maskId,
                        density: 0.82f,
                        feather: 1.2f))
            ]);
        PrismInstance instance = new(definition);
        BenchmarkFrame[] frames =
            new BenchmarkFrame[DynamicFrameCount];
        for (int index = 0;
            index < frames.Length;
            index++)
        {
            PrismDrawResources resources =
                PrismDrawResources.Create(
                [
                    new PrismDrawImageResource(
                        maskId,
                        image,
                        Version: index + 1,
                        Identity: 5_001)
                ]);
            PrismDrawScope scope = CreateScope(
                instance,
                ownerToken: 5_001,
                visualContentVersion: 1,
                resources: resources);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(238, 188, 63, 230));
        }

        return new BenchmarkScenario(
            "changed-resource",
            frames,
            new PrismRendererOptions(),
            image);
    }

    private static BenchmarkScenario
        CreateManyCommonInstancesScenario()
    {
        PrismCompositionDefinition definition =
            CreateFilteredControlDefinition("Common instances");
        DrawCommandList commands = new();
        for (int index = 0;
            index < CommonInstanceCount;
            index++)
        {
            PrismInstance instance = new(definition);
            PrismDrawScope scope = CreateScope(
                instance,
                ownerToken: 6_000 + index + 1,
                visualContentVersion: 1);
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(
                DrawCommand.FillRectangle(
                    new DrawRect(
                        0,
                        0,
                        SurfaceWidth,
                        SurfaceHeight),
                    new CernealaColor(
                        (byte)(48 + (index * 7)),
                        (byte)(216 - (index * 5)),
                        (byte)(96 + (index * 3)),
                        224)));
            commands.Add(DrawCommand.EndPrism());
        }

        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new BenchmarkScenario(
            "many-common-instances",
            [
                new BenchmarkFrame(
                    commands,
                    analysis,
                    plan,
                    BackdropLease: null)
            ],
            new PrismRendererOptions());
    }

    private static BenchmarkScenario CreateSmallBudgetScenario()
    {
        PrismCompositionDefinition definition =
            CreateFilteredControlDefinition(
                "Small budget churn");
        BenchmarkFrame[] frames =
            new BenchmarkFrame[DynamicFrameCount];
        for (int index = 0;
            index < frames.Length;
            index++)
        {
            PrismInstance instance = new(definition);
            PrismDrawScope scope = CreateScope(
                instance,
                ownerToken: 7_000 + index + 1,
                visualContentVersion: 1);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(184, 116, 236, 224));
        }

        return new BenchmarkScenario(
            "small-budget",
            frames,
            new PrismRendererOptions
            {
                SurfaceHardByteLimit = 64L * 1024 * 1024,
                RetainedCacheSoftByteLimit = 16L * 1024 * 1024,
                RetainedCacheEntryLimit = 4
            });
    }

    private static PrismCompositionDefinition
        CreateFilteredControlDefinition(string name) =>
        new(
            name,
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Content",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur)
                    ])
            ]);

    private static PrismDrawScope CreateScope(
        PrismInstance instance,
        long ownerToken,
        long visualContentVersion,
        PrismDrawResources? resources = null,
        long lowerUiVersion = 0) =>
        new(
            instance,
            new PrismCacheOwnerToken(ownerToken),
            new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight),
            System.Numerics.Matrix3x2.Identity,
            1,
            visualContentVersion,
            resources ?? PrismDrawResources.Empty,
            lowerUiVersion);

    private static BenchmarkFrame CreateControlFrame(
        PrismDrawScope scope,
        CernealaColor color)
    {
        DrawCommandList commands =
            CreateCommands(scope, color);
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new BenchmarkFrame(
            commands,
            analysis,
            plan,
            BackdropLease: null);
    }

    private static DrawCommandList CreateCommands(
        PrismDrawScope scope,
        CernealaColor color)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(
            DrawCommand.FillRectangle(
                new DrawRect(
                    0,
                    0,
                    SurfaceWidth,
                    SurfaceHeight),
                color));
        commands.Add(DrawCommand.EndPrism());
        return commands;
    }

    private static Texture2D CreateBackdropTexture(
        GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight,
            false,
            SurfaceFormat.Color);
        XnaColor[] pixels =
            new XnaColor[SurfaceWidth * SurfaceHeight];
        for (int y = 0; y < SurfaceHeight; y++)
        {
            for (int x = 0; x < SurfaceWidth; x++)
            {
                pixels[(y * SurfaceWidth) + x] =
                    new XnaColor(
                        (byte)(24 + (x * 128 / SurfaceWidth)),
                        (byte)(52 + (y * 144 / SurfaceHeight)),
                        (byte)(196 - (x * 96 / SurfaceWidth)),
                        byte.MaxValue);
            }
        }
        texture.SetData(pixels);
        return texture;
    }

    private enum BenchmarkScenarioKind
    {
        StaticControl,
        StaticBackdrop,
        AnimatedGameBackdrop,
        MotionParameter,
        ChangedResource,
        ManyCommonInstances,
        SmallBudget
    }

    private readonly record struct BenchmarkFrame(
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan,
        IBackdropFrameLease? BackdropLease);

    private sealed class BenchmarkScenario : IDisposable
    {
        private readonly IDisposable? ownedResource;

        public BenchmarkScenario(
            string name,
            BenchmarkFrame[] frames,
            PrismRendererOptions options,
            IDisposable? ownedResource = null)
        {
            Name = name;
            Frames = frames;
            Options = options;
            this.ownedResource = ownedResource;
        }

        public string Name { get; }

        public BenchmarkFrame[] Frames { get; }

        public PrismRendererOptions Options { get; }

        public BenchmarkFrame GetFrame(int index) =>
            Frames[index % Frames.Length];

        public void Dispose()
        {
            ownedResource?.Dispose();
        }
    }

    private sealed class BorrowedBackdropLease :
        IMonoGameBackdropFrameLease
    {
        public BorrowedBackdropLease(
            Texture2D texture,
            BackdropFrameMetadata metadata)
        {
            Texture = texture;
            Metadata = metadata;
        }

        public Texture2D Texture { get; }

        public BackdropFrameMetadata Metadata { get; }

        public void Dispose()
        {
        }
    }

    private sealed class BenchmarkRenderer :
        IPrismCommandRenderer,
        IDisposable
    {
        private readonly SpriteBatch spriteBatch;
        private readonly Texture2D whitePixel;
        private readonly RenderTarget2D hostTarget;
        private readonly XnaColor[] readback;
        private bool batchActive;

        public BenchmarkRenderer(
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
            readback = new XnaColor[width * height];
        }

        public GraphicsDevice GraphicsDevice { get; }

        public void BeginFrame()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(hostTarget);
            GraphicsDevice.Clear(XnaColor.Transparent);
            BeginCommandBatch();
        }

        public void Synchronize()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(null);
            hostTarget.GetData(readback);
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
                    $"Unsupported Prism benchmark command '{command.Kind}'.");
            }

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
            GraphicsDevice.Viewport = new Viewport(
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
                    "The Prism benchmark SpriteBatch is already active.");
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
                        $"Cerneala retained cache benchmark {Guid.NewGuid():N}",
                    Width = SurfaceWidth,
                    Height = SurfaceHeight
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as
                WindowsDxWindowGraphicsSession ??
                throw new InvalidOperationException(
                    "The benchmark window did not create a WindowsDX session.");
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
        public void RequestClose()
        {
        }

        public void ActivationChanged(bool active)
        {
        }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested()
        {
        }
    }
}

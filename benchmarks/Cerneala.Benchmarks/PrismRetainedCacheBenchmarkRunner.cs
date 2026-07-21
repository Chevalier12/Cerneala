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
    private const int WarmupFrameCount = 12;
    private const int MeasuredFrameCount = 96;
    private const int CompletionFrameCount = 8;
    private const int DynamicFrameCount =
        WarmupFrameCount + MeasuredFrameCount +
        CompletionFrameCount + 1;
    private const int CommonInstanceCount = 24;
    private static readonly BenchmarkResolution[] Resolutions =
    [
        new("preview", 256, 144),
        new("medium", 640, 360)
    ];

    public static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The Prism retained-cache benchmark requires WindowsDX.");
        }

        BenchmarkResolution largest = Resolutions[^1];
        using WindowsDxFixture fixture = new(largest.Width, largest.Height);
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

        foreach (BenchmarkResolution resolution in Resolutions)
        {
            foreach (BenchmarkScenarioKind kind in
                Enum.GetValues<BenchmarkScenarioKind>())
            {
                using BenchmarkScenario scenario =
                    CreateScenario(graphicsDevice, kind, resolution);
                RunScenario(
                    graphicsDevice,
                    scenario,
                    resolution,
                    retainedCacheEnabled: false);
                RunScenario(
                    graphicsDevice,
                    scenario,
                    resolution,
                    retainedCacheEnabled: true);
            }
        }
    }

    private static void RunScenario(
        GraphicsDevice graphicsDevice,
        BenchmarkScenario scenario,
        BenchmarkResolution resolution,
        bool retainedCacheEnabled)
    {
        using BenchmarkRenderer renderer = new(
            graphicsDevice,
            resolution.Width,
            resolution.Height);
        PrismExecutionDiagnostics executionDiagnostics = new();
        using PrismGraphExecutor executor = new(
            graphicsDevice,
            executionDiagnostics,
            scenario.Options,
            retainedCacheEnabled);
        Viewport viewport = new(
            0,
            0,
            resolution.Width,
            resolution.Height);

        for (int frame = 0; frame < WarmupFrameCount; frame++)
        {
            GC.KeepAlive(scenario.BuildPlan());
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long buildAllocationStart =
            GC.GetAllocatedBytesForCurrentThread();
        long buildStarted = Stopwatch.GetTimestamp();
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            GC.KeepAlive(scenario.BuildPlan());
        }
        TimeSpan buildElapsed =
            Stopwatch.GetElapsedTime(buildStarted);
        long buildAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() -
            buildAllocationStart;

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
        long executedPasses = 0;
        long captures = 0;
        long createdSurfaces = 0;
        long reusedSurfaces = 0;
        long fallbacks = 0;
        int peakLiveSurfaces = 0;
        int activeSurfaces = 0;
        long peakSurfaceBytes = 0;
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
            PrismExecutionCounters counters =
                executionDiagnostics.Counters;
            executedPasses += counters.PassCount;
            captures += counters.CaptureCount;
            createdSurfaces += counters.CreatedSurfaceCount;
            reusedSurfaces += counters.ReusedSurfaceCount;
            fallbacks += counters.FallbackCount;
            peakLiveSurfaces = Math.Max(
                peakLiveSurfaces,
                counters.PeakLiveSurfaceCount);
            activeSurfaces = Math.Max(
                activeSurfaces,
                counters.ActiveSurfaceCount);
            peakSurfaceBytes = Math.Max(
                peakSurfaceBytes,
                counters.PeakSurfaceByteCount);
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

        long finalHits =
            after.FinalHitCount - before.FinalHitCount;
        long lookups =
            after.LookupCount - before.LookupCount;
        long savedCaptures =
            after.SavedCaptureCount - before.SavedCaptureCount;
        ValidateScenario(
            scenario,
            retainedCacheEnabled,
            allocatedBytes,
            finalHits,
            lookups,
            savedCaptures,
            fallbacks,
            activeSurfaces);

        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"PRISM_RETAINED_BENCHMARK " +
                $"scenario={scenario.Name} " +
                $"resolution={resolution.Name} " +
                $"width={resolution.Width} " +
                $"height={resolution.Height} " +
                $"cache={(retainedCacheEnabled ? "on" : "off")} " +
                $"frames={MeasuredFrameCount} " +
                $"cpu-build-us=" +
                $"{buildElapsed.TotalMicroseconds / MeasuredFrameCount:F3} " +
                $"build-allocated-bytes={buildAllocatedBytes} " +
                $"cpu-submit-us=" +
                $"{cpuElapsed.TotalMicroseconds / MeasuredFrameCount:F3} " +
                $"gpu-completion-upper-bound-us=" +
                $"{completionUpperBound.TotalMicroseconds / CompletionFrameCount:F3} " +
                $"allocated-bytes={allocatedBytes} " +
                $"passes={executedPasses} " +
                $"captures={captures} " +
                $"peak-live-surfaces={peakLiveSurfaces} " +
                $"created-surfaces={createdSurfaces} " +
                $"reused-surfaces={reusedSurfaces} " +
                $"peak-surface-bytes={peakSurfaceBytes} " +
                $"fallbacks={fallbacks} " +
                $"final-hits={finalHits} " +
                $"intermediate-hits=" +
                $"{after.IntermediateHitCount - before.IntermediateHitCount} " +
                $"misses={after.MissCount - before.MissCount} " +
                $"lookups={lookups} " +
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
                $"saved-captures={savedCaptures} " +
                $"saved-passes=" +
                $"{after.SavedPassCount - before.SavedPassCount}"));
    }

    private static void ValidateScenario(
        BenchmarkScenario scenario,
        bool retainedCacheEnabled,
        long allocatedBytes,
        long finalHits,
        long lookups,
        long savedCaptures,
        long fallbacks,
        int activeSurfaces)
    {
        if (fallbacks != 0 || activeSurfaces != 0)
        {
            throw new InvalidOperationException(
                $"Benchmark '{scenario.Name}' left {activeSurfaces} active surfaces " +
                $"and reported {fallbacks} fallback(s).");
        }

        if (!retainedCacheEnabled && lookups != 0)
        {
            throw new InvalidOperationException(
                $"Cache-off benchmark '{scenario.Name}' performed {lookups} retained lookup(s).");
        }

        if (retainedCacheEnabled && scenario.ExpectStaticRetainedHit &&
            (finalHits == 0 || savedCaptures == 0 || allocatedBytes != 0))
        {
            throw new InvalidOperationException(
                $"Static benchmark '{scenario.Name}' expected retained hits, saved captures, " +
                $"and zero managed allocation; observed hits={finalHits}, " +
                $"savedCaptures={savedCaptures}, allocatedBytes={allocatedBytes}.");
        }
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
        BenchmarkScenarioKind kind,
        BenchmarkResolution resolution) =>
        kind switch
        {
            BenchmarkScenarioKind.StaticControl =>
                CreateStaticControlScenario(resolution),
            BenchmarkScenarioKind.StaticBackdrop =>
                CreateBackdropScenario(
                    graphicsDevice,
                    resolution,
                    animated: false),
            BenchmarkScenarioKind.AnimatedGameBackdrop =>
                CreateBackdropScenario(
                    graphicsDevice,
                    resolution,
                    animated: true),
            BenchmarkScenarioKind.MotionParameter =>
                CreateMotionParameterScenario(resolution),
            BenchmarkScenarioKind.ChangedResource =>
                CreateChangedResourceScenario(graphicsDevice, resolution),
            BenchmarkScenarioKind.ManyCommonInstances =>
                CreateManyCommonInstancesScenario(resolution),
            BenchmarkScenarioKind.SmallBudget =>
                CreateSmallBudgetScenario(resolution),
            BenchmarkScenarioKind.ManyLayers =>
                CreateManyLayersScenario(resolution),
            BenchmarkScenarioKind.FilterChain =>
                CreateFilterChainScenario(resolution),
            BenchmarkScenarioKind.Styles =>
                CreateStylesScenario(resolution),
            BenchmarkScenarioKind.NestedGroups =>
                CreateNestedGroupsScenario(resolution),
            BenchmarkScenarioKind.SharedBackdrop =>
                CreateSharedBackdropScenario(graphicsDevice, resolution),
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown Prism benchmark scenario.")
        };

    private static BenchmarkScenario
        CreateStaticControlScenario(BenchmarkResolution resolution)
    {
        PrismCompositionDefinition definition =
            CreateFilteredControlDefinition("Static control");
        PrismInstance instance = new(definition);
        PrismDrawScope scope = CreateScope(
            instance,
            ownerToken: 1_001,
            visualContentVersion: 1,
            resolution);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(69, 145, 232, 224),
            resolution);
        return new BenchmarkScenario(
            "static-control",
            [frame],
            new PrismRendererOptions(),
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateBackdropScenario(
        GraphicsDevice graphicsDevice,
        BenchmarkResolution resolution,
        bool animated)
    {
        Texture2D texture = CreateBackdropTexture(graphicsDevice, resolution);
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
            resolution,
            lowerUiVersion: 1);
        DrawCommandList commands = CreateCommands(
            scope,
            new CernealaColor(232, 78, 102, 192),
            resolution);
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
                resolution.Width,
                resolution.Height,
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
            () => BuildBackdropPlan(
                commands,
                frames[0].BackdropLease!.Metadata,
                sourceToken),
            expectStaticRetainedHit: !animated,
            texture);
    }

    private static BenchmarkScenario
        CreateMotionParameterScenario(BenchmarkResolution resolution)
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
                visualContentVersion: index + 1,
                resolution);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(82, 204, 142, 224),
                resolution);
        }

        return new BenchmarkScenario(
            "motion-parameter",
            frames,
            new PrismRendererOptions(),
            () => BuildPlan(frames[0].Commands),
            expectStaticRetainedHit: false);
    }

    private static BenchmarkScenario
        CreateChangedResourceScenario(
            GraphicsDevice graphicsDevice,
            BenchmarkResolution resolution)
    {
        Texture2D texture = new(
            graphicsDevice,
            resolution.Width,
            resolution.Height,
            false,
            SurfaceFormat.Color);
        XnaColor[] maskPixels =
            new XnaColor[resolution.Width * resolution.Height];
        for (int y = 0; y < resolution.Height; y++)
        {
            for (int x = 0; x < resolution.Width; x++)
            {
                byte alpha = (byte)(48 +
                    ((x + y) % 176));
                maskPixels[(y * resolution.Width) + x] =
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
                resolution,
                resources: resources);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(238, 188, 63, 230),
                resolution);
        }

        return new BenchmarkScenario(
            "changed-resource",
            frames,
            new PrismRendererOptions(),
            () => BuildPlan(frames[0].Commands),
            expectStaticRetainedHit: false,
            image);
    }

    private static BenchmarkScenario
        CreateManyCommonInstancesScenario(BenchmarkResolution resolution)
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
                visualContentVersion: 1,
                resolution);
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(
                DrawCommand.FillRectangle(
                    new DrawRect(
                        0,
                        0,
                        resolution.Width,
                        resolution.Height),
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
            BuildPlan(analysis);
        return new BenchmarkScenario(
            "many-common-instances",
            [
                new BenchmarkFrame(
                    commands,
                    analysis,
                    plan,
                    BackdropLease: null)
            ],
            new PrismRendererOptions(),
            () => BuildPlan(commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateSmallBudgetScenario(
        BenchmarkResolution resolution)
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
                visualContentVersion: 1,
                resolution);
            frames[index] = CreateControlFrame(
                scope,
                new CernealaColor(184, 116, 236, 224),
                resolution);
        }

        return new BenchmarkScenario(
            "small-budget",
            frames,
            new PrismRendererOptions
            {
                SurfaceHardByteLimit = 64L * 1024 * 1024,
                RetainedCacheSoftByteLimit = 16L * 1024 * 1024,
                RetainedCacheEntryLimit = 4
            },
            () => BuildPlan(frames[0].Commands),
            expectStaticRetainedHit: false);
    }

    private static BenchmarkScenario CreateManyLayersScenario(
        BenchmarkResolution resolution)
    {
        PrismNodeDefinition[] layers = Enumerable.Range(0, 12)
            .Select(index => (PrismNodeDefinition)new PrismLayerDefinition(
                new PrismNodeId(index + 1),
                $"Layer {index + 1}",
                filters: [new PrismFilterDefinition(PrismFilterId.Invert)],
                opacity: 0.45f + (index * 0.04f)))
            .ToArray();
        PrismCompositionDefinition definition = new("Many layers", layers);
        PrismDrawScope scope = CreateScope(
            new PrismInstance(definition),
            ownerToken: 8_001,
            visualContentVersion: 1,
            resolution);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(56, 172, 214, 220),
            resolution);
        return new BenchmarkScenario(
            "many-layers",
            [frame],
            new PrismRendererOptions(),
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateFilterChainScenario(
        BenchmarkResolution resolution)
    {
        PrismCompositionDefinition definition = new(
            "Filter chain",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Chain",
                    filters:
                    [
                        new PrismFilterDefinition(PrismFilterId.GaussianBlur),
                        new PrismFilterDefinition(PrismFilterId.HueSaturation),
                        new PrismFilterDefinition(PrismFilterId.Invert),
                        new PrismFilterDefinition(PrismFilterId.BrightnessContrast)
                    ])
            ]);
        PrismDrawScope scope = CreateScope(
            new PrismInstance(definition),
            ownerToken: 9_001,
            visualContentVersion: 1,
            resolution);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(212, 88, 151, 228),
            resolution);
        return new BenchmarkScenario(
            "filter-chain",
            [frame],
            new PrismRendererOptions(),
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateStylesScenario(
        BenchmarkResolution resolution)
    {
        PrismCompositionDefinition definition = new(
            "Styles",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Styled",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)],
                    styles:
                    [
                        new PrismStyleDefinition(PrismStyleId.DropShadow),
                        new PrismStyleDefinition(PrismStyleId.OuterGlow)
                    ])
            ]);
        PrismDrawScope scope = CreateScope(
            new PrismInstance(definition),
            ownerToken: 10_001,
            visualContentVersion: 1,
            resolution);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(242, 176, 55, 220),
            resolution);
        return new BenchmarkScenario(
            "styles",
            [frame],
            new PrismRendererOptions(),
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateNestedGroupsScenario(
        BenchmarkResolution resolution)
    {
        PrismCompositionDefinition definition = new(
            "Nested groups",
            [
                new PrismGroupDefinition(
                    new PrismNodeId(20),
                    "Outer",
                    [
                        new PrismGroupDefinition(
                            new PrismNodeId(21),
                            "Inner",
                            [
                                new PrismLayerDefinition(
                                    new PrismNodeId(1),
                                    "Blurred",
                                    filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)]),
                                new PrismLayerDefinition(
                                    new PrismNodeId(2),
                                    "Inverted",
                                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])
                            ],
                            filters: [new PrismFilterDefinition(PrismFilterId.HueSaturation)])
                    ],
                    filters: [new PrismFilterDefinition(PrismFilterId.BrightnessContrast)])
            ]);
        PrismDrawScope scope = CreateScope(
            new PrismInstance(definition),
            ownerToken: 11_001,
            visualContentVersion: 1,
            resolution);
        BenchmarkFrame frame = CreateControlFrame(
            scope,
            new CernealaColor(96, 204, 132, 224),
            resolution);
        return new BenchmarkScenario(
            "nested-groups",
            [frame],
            new PrismRendererOptions(),
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateSharedBackdropScenario(
        GraphicsDevice graphicsDevice,
        BenchmarkResolution resolution)
    {
        Texture2D texture = CreateBackdropTexture(graphicsDevice, resolution);
        PrismCompositionDefinition definition = new(
            "Shared backdrop",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Foreground",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)]),
                new PrismBackdropDefinition(
                    new PrismNodeId(2),
                    "Shared host backdrop",
                    filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)])
            ]);
        DrawCommandList commands = new();
        for (int index = 0; index < 2; index++)
        {
            PrismDrawScope scope = CreateScope(
                new PrismInstance(definition),
                ownerToken: 12_001 + index,
                visualContentVersion: 1,
                resolution,
                lowerUiVersion: 1);
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(DrawCommand.FillRectangle(
                new DrawRect(0, 0, resolution.Width, resolution.Height),
                index == 0
                    ? new CernealaColor(68, 136, 238, 176)
                    : new CernealaColor(226, 92, 126, 176)));
            commands.Add(DrawCommand.EndPrism());
        }

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        BackdropFrameMetadata metadata = new(
            resolution.Width,
            resolution.Height,
            1,
            PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Opaque,
            System.Numerics.Matrix3x2.Identity,
            ContentVersion: 20_000);
        PrismBackdropSourceToken sourceToken = PrismBackdropSourceToken.CreateUnique();
        PrismGraphExecutionPlan plan = BuildBackdropPlan(commands, metadata, sourceToken);
        BenchmarkFrame frame = new(
            commands,
            analysis,
            plan,
            new BorrowedBackdropLease(texture, metadata));
        return new BenchmarkScenario(
            "shared-backdrop",
            [frame],
            new PrismRendererOptions(),
            () => BuildBackdropPlan(commands, metadata, sourceToken),
            expectStaticRetainedHit: true,
            texture);
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
        BenchmarkResolution resolution,
        PrismDrawResources? resources = null,
        long lowerUiVersion = 0) =>
        new(
            instance,
            new PrismCacheOwnerToken(ownerToken),
            new DrawRect(
                0,
                0,
                resolution.Width,
                resolution.Height),
            System.Numerics.Matrix3x2.Identity,
            1,
            visualContentVersion,
            resources ?? PrismDrawResources.Empty,
            lowerUiVersion);

    private static BenchmarkFrame CreateControlFrame(
        PrismDrawScope scope,
        CernealaColor color,
        BenchmarkResolution resolution)
    {
        DrawCommandList commands =
            CreateCommands(scope, color, resolution);
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            BuildPlan(analysis);
        return new BenchmarkFrame(
            commands,
            analysis,
            plan,
            BackdropLease: null);
    }

    private static DrawCommandList CreateCommands(
        PrismDrawScope scope,
        CernealaColor color,
        BenchmarkResolution resolution)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(
            DrawCommand.FillRectangle(
                new DrawRect(
                    0,
                    0,
                    resolution.Width,
                    resolution.Height),
                color));
        commands.Add(DrawCommand.EndPrism());
        return commands;
    }

    private static Texture2D CreateBackdropTexture(
        GraphicsDevice graphicsDevice,
        BenchmarkResolution resolution)
    {
        Texture2D texture = new(
            graphicsDevice,
            resolution.Width,
            resolution.Height,
            false,
            SurfaceFormat.Color);
        XnaColor[] pixels =
            new XnaColor[resolution.Width * resolution.Height];
        for (int y = 0; y < resolution.Height; y++)
        {
            for (int x = 0; x < resolution.Width; x++)
            {
                pixels[(y * resolution.Width) + x] =
                    new XnaColor(
                        (byte)(24 + (x * 128 / resolution.Width)),
                        (byte)(52 + (y * 144 / resolution.Height)),
                        (byte)(196 - (x * 96 / resolution.Width)),
                        byte.MaxValue);
            }
        }
        texture.SetData(pixels);
        return texture;
    }

    private static PrismGraphExecutionPlan BuildPlan(
        DrawCommandList commands) =>
        BuildPlan(new PrismFrameAnalyzer().Analyze(commands));

    private static PrismGraphExecutionPlan BuildPlan(
        PrismFrameAnalysis analysis) =>
        new PrismGraphOptimizer().Optimize(
            new PrismGraphBuilder().Build(analysis));

    private static PrismGraphExecutionPlan BuildBackdropPlan(
        DrawCommandList commands,
        BackdropFrameMetadata metadata,
        PrismBackdropSourceToken sourceToken) =>
        new PrismGraphOptimizer().Optimize(
            new PrismGraphBuilder().Build(
                new PrismFrameAnalyzer().Analyze(commands),
                metadata,
                sourceToken));

    private enum BenchmarkScenarioKind
    {
        StaticControl,
        StaticBackdrop,
        AnimatedGameBackdrop,
        MotionParameter,
        ChangedResource,
        ManyCommonInstances,
        SmallBudget,
        ManyLayers,
        FilterChain,
        Styles,
        NestedGroups,
        SharedBackdrop
    }

    private readonly record struct BenchmarkResolution(
        string Name,
        int Width,
        int Height);

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
            Func<PrismGraphExecutionPlan> buildPlan,
            bool expectStaticRetainedHit,
            IDisposable? ownedResource = null)
        {
            Name = name;
            Frames = frames;
            Options = options;
            BuildPlan = buildPlan;
            ExpectStaticRetainedHit = expectStaticRetainedHit;
            this.ownedResource = ownedResource;
        }

        public string Name { get; }

        public BenchmarkFrame[] Frames { get; }

        public PrismRendererOptions Options { get; }

        public Func<PrismGraphExecutionPlan> BuildPlan { get; }

        public bool ExpectStaticRetainedHit { get; }

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

        public WindowsDxFixture(int width, int height)
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala retained cache benchmark {Guid.NewGuid():N}",
                    Width = width,
                    Height = height
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

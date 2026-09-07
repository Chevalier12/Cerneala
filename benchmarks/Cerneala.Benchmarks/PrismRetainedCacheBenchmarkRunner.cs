using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;
using System.Diagnostics;
using System.Globalization;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using CernealaColor = Cerneala.Drawing.Color;

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
        foreach (BenchmarkResolution resolution in Resolutions)
        {
            using SdlGpuFixture fixture = new(resolution.Width, resolution.Height);
            Console.WriteLine($"PRISM_RETAINED_HARDWARE backend=SDL_GPU format={fixture.Session.Diagnostics.TextureFormat} processors={Environment.ProcessorCount} os=\"{Environment.OSVersion.VersionString}\"");
            foreach (BenchmarkScenarioKind kind in Enum.GetValues<BenchmarkScenarioKind>())
            {
                using BenchmarkScenario scenario = CreateScenario(fixture.Session, kind, resolution);
                RunScenario(fixture.Session, scenario, resolution);
                fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
            }
        }
    }

    private static void RunScenario(
        SdlGpuWindowGraphicsSession session,
        BenchmarkScenario scenario,
        BenchmarkResolution resolution)
    {
        for (int frame = 0; frame < WarmupFrameCount; frame++)
        {
            GC.KeepAlive(scenario.BuildPlan());
        }
        Collect();
        long buildAllocationStart = GC.GetAllocatedBytesForCurrentThread();
        long buildStarted = Stopwatch.GetTimestamp();
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            GC.KeepAlive(scenario.BuildPlan());
        }
        TimeSpan buildElapsed = Stopwatch.GetElapsedTime(buildStarted);
        long buildAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - buildAllocationStart;

        for (int frame = 0; frame <= WarmupFrameCount; frame++)
        {
            ExecuteFrame(session, scenario.GetFrame(frame), scenario.BackdropSourceToken);
        }
        Synchronize(session);
        Collect();
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        long cpuStarted = Stopwatch.GetTimestamp();
        long passes = 0, plannedPasses = 0, captures = 0, created = 0, reused = 0, fallbacks = 0;
        int peakLive = 0, active = 0;
        long peakBytes = 0;
        PrismExecutionDiagnostics diagnostics = ((IWindowGraphicsSession)session).PrismExecutionDiagnostics!;
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            ExecuteFrame(session, scenario.GetFrame(WarmupFrameCount + frame + 1), scenario.BackdropSourceToken);
            PrismExecutionCounters counters = diagnostics.Counters;
            passes += counters.PassCount;
            plannedPasses += counters.PlannedPassCount;
            captures += counters.CaptureCount;
            created += counters.CreatedSurfaceCount;
            reused += counters.ReusedSurfaceCount;
            fallbacks += counters.FallbackCount;
            peakLive = Math.Max(peakLive, counters.PeakLiveSurfaceCount);
            active = Math.Max(active, counters.ActiveSurfaceCount);
            peakBytes = Math.Max(peakBytes, counters.PeakSurfaceByteCount);
        }
        TimeSpan cpuElapsed = Stopwatch.GetElapsedTime(cpuStarted);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        long completionStarted = Stopwatch.GetTimestamp();
        for (int frame = 0; frame < CompletionFrameCount; frame++)
        {
            ExecuteFrame(session, scenario.GetFrame(WarmupFrameCount + MeasuredFrameCount + frame + 1),
                scenario.BackdropSourceToken);
            Synchronize(session);
        }
        TimeSpan completionUpperBound = Stopwatch.GetElapsedTime(completionStarted);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"PRISM_RETAINED_BENCHMARK_V2 backend=SDL_GPU scenario={scenario.Name} " +
            $"resolution={resolution.Name} width={resolution.Width} height={resolution.Height} " +
            $"configuration=internal frames={MeasuredFrameCount} " +
            $"cpu-build-us={buildElapsed.TotalMicroseconds / MeasuredFrameCount:F3} " +
            $"build-allocated-bytes={buildAllocatedBytes} cpu-frame-submit-us={cpuElapsed.TotalMicroseconds / MeasuredFrameCount:F3} " +
            $"gpu-completion-upper-bound-us={completionUpperBound.TotalMicroseconds / CompletionFrameCount:F3} " +
            $"frame-allocated-bytes={allocatedBytes} passes={passes} planned-passes={plannedPasses} captures={captures} " +
            $"peak-live-surfaces={peakLive} created-surfaces={created} reused-surfaces={reused} " +
            $"peak-surface-bytes={peakBytes} fallbacks={fallbacks} active-surfaces={active} " +
            $"retained-entries={session.DrawingResources.PrismResources.RetainedCount}"));
        if (fallbacks != 0 || active != 0)
        {
            throw new InvalidOperationException($"Benchmark '{scenario.Name}' left {active} active surfaces and {fallbacks} fallbacks.");
        }
        if (scenario.ExpectStaticRetainedHit && passes >= plannedPasses)
        {
            throw new InvalidOperationException($"Static benchmark '{scenario.Name}' saved no Prism passes after warmup.");
        }
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void Synchronize(SdlGpuWindowGraphicsSession session)
    {
        nint commandBuffer = session.Api.AcquireGpuCommandBuffer(session.Device);
        if (commandBuffer == 0)
        {
            throw SdlApiError.Create(session.Api, "Benchmark GPU synchronization command buffer");
        }
        nint fence = session.Api.SubmitGpuCommandBufferAndAcquireFence(commandBuffer);
        if (fence == 0)
        {
            throw SdlApiError.Create(session.Api, "Benchmark GPU synchronization fence");
        }
        try
        {
            if (!session.Api.WaitForGpuFence(session.Device, fence))
            {
                throw SdlApiError.Create(session.Api, "Benchmark GPU synchronization wait");
            }
        }
        finally
        {
            session.Api.ReleaseGpuFence(session.Device, fence);
        }
    }

    private static void ExecuteFrame(SdlGpuWindowGraphicsSession session, BenchmarkFrame frame,
        PrismBackdropSourceToken sourceToken)
    {
        session.BeginFrame(CernealaColor.Transparent);
        try
        {
            DrawingFrameContext context = new(frame.Analysis, frame.BackdropLease,
                frame.BackdropLease is null ? default : sourceToken);
            session.DrawingBackend.Render(frame.Commands, in context);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }
    private static BenchmarkScenario CreateScenario(
        SdlGpuWindowGraphicsSession session,
        BenchmarkScenarioKind kind,
        BenchmarkResolution resolution) =>
        kind switch
        {
            BenchmarkScenarioKind.StaticControl =>
                CreateStaticControlScenario(resolution),
            BenchmarkScenarioKind.StaticBackdrop =>
                CreateBackdropScenario(
                    session,
                    resolution,
                    animated: false),
            BenchmarkScenarioKind.AnimatedGameBackdrop =>
                CreateBackdropScenario(
                    session,
                    resolution,
                    animated: true),
            BenchmarkScenarioKind.MotionParameter =>
                CreateMotionParameterScenario(resolution),
            BenchmarkScenarioKind.ChangedResource =>
                CreateChangedResourceScenario(session, resolution),
            BenchmarkScenarioKind.ManyCommonInstances =>
                CreateManyCommonInstancesScenario(resolution),
            BenchmarkScenarioKind.ManyLayers =>
                CreateManyLayersScenario(resolution),
            BenchmarkScenarioKind.FilterChain =>
                CreateFilterChainScenario(resolution),
            BenchmarkScenarioKind.Styles =>
                CreateStylesScenario(resolution),
            BenchmarkScenarioKind.NestedGroups =>
                CreateNestedGroupsScenario(resolution),
            BenchmarkScenarioKind.SharedBackdrop =>
                CreateSharedBackdropScenario(session, resolution),
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
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateBackdropScenario(
        SdlGpuWindowGraphicsSession session,
        BenchmarkResolution resolution,
        bool animated)
    {
        BenchmarkBackdrop texture = CreateBackdropTexture(session, resolution);
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
                new PrismLayerDefinition(
                    new PrismNodeId(2),
                    "Host backdrop",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur)
                    ],
                    blendMode: PrismBlendMode.Multiply)
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
            () => BuildPlan(frames[0].Commands),
            expectStaticRetainedHit: false);
    }

    private static BenchmarkScenario
        CreateChangedResourceScenario(
            SdlGpuWindowGraphicsSession session,
            BenchmarkResolution resolution)
    {
        byte[] maskPixels = new byte[resolution.Width * resolution.Height * 4];
        for (int y = 0; y < resolution.Height; y++)
        {
            for (int x = 0; x < resolution.Width; x++)
            {
                byte alpha = (byte)(48 + ((x + y) % 176));
                maskPixels.AsSpan(((y * resolution.Width) + x) * 4, 4).Fill(alpha);
            }
        }
        SdlGpuImage image = new(resolution.Width, resolution.Height, maskPixels);        PrismResourceId maskId = new("BenchmarkMask");
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
            () => BuildPlan(commands),
            expectStaticRetainedHit: true);
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
            () => BuildPlan(frame.Commands),
            expectStaticRetainedHit: true);
    }

    private static BenchmarkScenario CreateSharedBackdropScenario(
        SdlGpuWindowGraphicsSession session,
        BenchmarkResolution resolution)
    {
        BenchmarkBackdrop texture = CreateBackdropTexture(session, resolution);
        PrismCompositionDefinition definition = new(
            "Shared backdrop",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Foreground",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)]),
                new PrismLayerDefinition(
                    new PrismNodeId(2),
                    "Shared host backdrop",
                    filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)],
                    blendMode: PrismBlendMode.Multiply)
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

    private static BenchmarkBackdrop CreateBackdropTexture(
        SdlGpuWindowGraphicsSession session, BenchmarkResolution resolution)
    {
        byte[] pixels = new byte[resolution.Width * resolution.Height * 4];
        for (int y = 0; y < resolution.Height; y++)
        {
            for (int x = 0; x < resolution.Width; x++)
            {
                int offset = ((y * resolution.Width) + x) * 4;
                pixels[offset] = (byte)(24 + (x * 128 / resolution.Width));
                pixels[offset + 1] = (byte)(52 + (y * 144 / resolution.Height));
                pixels[offset + 2] = (byte)(196 - (x * 96 / resolution.Width));
                pixels[offset + 3] = 255;
            }
        }
        SdlGpuImage image = new(resolution.Width, resolution.Height, pixels);
        session.BeginFrame(CernealaColor.Transparent);
        try
        {
            nint texture = session.DrawingResources.GetOrCreateTexture(session, image,
                resolution.Width, resolution.Height, image.RgbaPixels.Span).Handle;
            return new BenchmarkBackdrop(image, texture);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    private sealed record BenchmarkBackdrop(SdlGpuImage Image, nint Texture) : IDisposable
    {
        public void Dispose() => Image.Dispose();
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
            Func<PrismGraphExecutionPlan> buildPlan,
            bool expectStaticRetainedHit,
            IDisposable? ownedResource = null)
        {
            Name = name;
            Frames = frames;
            BuildPlan = buildPlan;
            ExpectStaticRetainedHit = expectStaticRetainedHit;
            this.ownedResource = ownedResource;
        }

        public string Name { get; }

        public BenchmarkFrame[] Frames { get; }

        public PrismBackdropSourceToken BackdropSourceToken { get; } = PrismBackdropSourceToken.CreateUnique();

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
        ISdlGpuBackdropFrameLease
    {
        public BorrowedBackdropLease(
            BenchmarkBackdrop texture,
            BackdropFrameMetadata metadata)
        {
            Texture = texture.Texture;
            Metadata = metadata;
        }

        public nint Texture { get; }

        public BackdropFrameMetadata Metadata { get; }

        public void Dispose()
        {
        }
    }

    private sealed class SdlGpuFixture : IDisposable
    {
        private readonly NativeSdlApi api = new();
        private readonly SdlGpuWindowGraphicsSessionFactory graphics;
        private readonly SdlWindowPlatform platform;
        private readonly IPlatformWindow window;

        public SdlGpuFixture(int width, int height)
        {
            graphics = new(api, useMultisampling: false);
            platform = new(api, graphics, coordinateScaleOverride: 1);
            window = platform.CreateWindow(new Window
            {
                Title = "Cerneala retained cache SDL_GPU benchmark", Width = width, Height = height
            }, new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ??
                throw new InvalidOperationException("The benchmark window did not create an SDL_GPU session.");
        }

        public SdlGpuWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
            graphics.Dispose();
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

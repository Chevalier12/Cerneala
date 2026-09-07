using System.Diagnostics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;
using Xunit.Abstractions;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismExecutionBudgetMigrationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmGraphConstructionAndOptimizationDoNotAllocate(bool optimize)
    {
        DrawCommandList commands = Scene("simple");
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphBuilder builder = new();
        PrismGraph graph = builder.Build(analysis);
        PrismGraphOptimizer optimizer = new();
        for (int iteration = 0; iteration < 8; iteration++)
        {
            if (optimize) optimizer.Optimize(graph);
            else builder.Build(analysis);
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 64; iteration++)
        {
            if (optimize) optimizer.Optimize(graph);
            else builder.Build(analysis);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    [SdlNativeTheory]
    [InlineData("simple")]
    [InlineData("styles")]
    [InlineData("chained")]
    [InlineData("nested")]
    public void WarmRetainedExecutionPreservesTheZeroAllocationAndReuseBudgets(string scenario)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuPrismExecutor executor = new(fixture.Session, fixture.Backend);
        DrawCommandList commands = Scene(scenario);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext context = new(analysis);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(analysis));
        if (scenario == "styles")
        {
            Assert.Equal(48, plan.OptimizedGraph.Nodes.Count(node => node.Kind == PrismGraphNodeKind.Style));
            Assert.InRange(plan.PeakLiveSurfaces, 1, 47);
        }
        void Render()
        {
            fixture.Session.BeginFrame(Color.White);
            try { executor.Execute(commands, in context); }
            finally { fixture.Session.CompleteFrame(present: false); }
        }
        Render();
        Assert.True(executor.Diagnostics.Counters.PeakLiveSurfaceCount > 0);
        for (int frame = 1; frame < 8; frame++) Render();
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        long created = resources.CreatedSurfaceCount;
        long reused = resources.ReusedSurfaceCount;
        Assert.True(created > 0);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Render();

        long allocated = 0;
        long submitTicks = 0;
        for (int frame = 0; frame < 16; frame++)
        {
            fixture.Session.BeginFrame(Color.White);
            try
            {
                // Match the former executor-only gate: no frame analysis, readback,
                // native command-buffer acquisition or completion in the measurement.
                long before = GC.GetAllocatedBytesForCurrentThread();
                executor.Execute(commands, in context);
                allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                submitTicks += executor.Diagnostics.Counters.CpuSubmitTime.Ticks;
            }
            finally { fixture.Session.CompleteFrame(present: false); }
        }
        PrismExecutionCounters counters = executor.Diagnostics.Counters;
        long completionStarted = Stopwatch.GetTimestamp();
        Render();
        byte[] pixels = fixture.Session.CapturePresentedFrame().Pixels;
        TimeSpan completionUpperBound = Stopwatch.GetElapsedTime(completionStarted);
        output.WriteLine($"PRISM_PROFILE name={scenario} allocated-bytes={allocated} " +
            $"passes={counters.PassCount} captures={counters.CaptureCount} " +
            $"peak={counters.PeakLiveSurfaceCount} created={resources.CreatedSurfaceCount} " +
            $"reused={resources.ReusedSurfaceCount} cpu-submit-us={TimeSpan.FromTicks(submitTicks / 16).TotalMicroseconds:F3} " +
            $"gpu-completion-upper-bound-us={completionUpperBound.TotalMicroseconds:F3}");
        Assert.Equal(plan.OptimizedGraph.Scopes.Count(scope => scope.Depth == 0 && scope.Output.HasValue), counters.PassCount);
        Assert.Equal(0, counters.CaptureCount);
        Assert.Equal(0, allocated);
        Assert.Equal(created, resources.CreatedSurfaceCount);
        Assert.Equal(reused, resources.ReusedSurfaceCount);
        Assert.Equal(0, counters.PeakLiveSurfaceCount);
        Assert.Equal(0, executor.Diagnostics.Count);
        Assert.True(submitTicks > 0);
        Assert.True(completionUpperBound > TimeSpan.Zero);
        if (scenario == "simple")
        {
            int center = (8 * 16 + 8) * 4;
            for (int channel = 0; channel < 4; channel++) Assert.InRange(pixels[center + channel], (byte)254, (byte)255);
        }
        resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(resources.TotalBytes, resources.FreeBytes);
    }

    [SdlNativeFact]
    public void TwoThousandFortyEightAnimatedFramesReuseSurfacesAndShaderPipelines()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuPrismExecutor executor = new(fixture.Session, fixture.Backend);
        DrawCommandList low = Scene("simple", 0.25f);
        DrawCommandList high = Scene("simple", 0.75f);
        PrismFrameAnalysis lowAnalysis = new PrismFrameAnalyzer().Analyze(low);
        PrismFrameAnalysis highAnalysis = new PrismFrameAnalyzer().Analyze(high);
        DrawingFrameContext lowContext = new(lowAnalysis);
        DrawingFrameContext highContext = new(highAnalysis);
        PrismGraphExecutionPlan lowPlan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(lowAnalysis));
        PrismGraphExecutionPlan highPlan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(highAnalysis));
        void Render(bool lower)
        {
            fixture.Session.BeginFrame(Color.Transparent);
            try
            {
                if (lower) executor.Execute(low, in lowContext);
                else executor.Execute(high, in highContext);
            }
            finally { fixture.Session.CompleteFrame(present: false); }
        }
        for (int frame = 0; frame < 8; frame++) Render(frame % 2 == 0);
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        nint graphPipeline = resources.GetPipeline(SdlGpuTextureFormat.R16G16B16A16Float);
        nint PresentationPipeline() => fixture.Session.DrawingResources.GetPipeline(
            fixture.Session.WindowRenderTarget.ColorFormat,
            fixture.Session.WindowRenderTarget.SampleCount,
            DrawPrimitiveTopology.TriangleList, DrawBlendMode.Normal,
            SdlGpuStencilMode.Disabled, prismPresentation: true);
        nint presentationPipeline = PresentationPipeline();
        long created = resources.CreatedSurfaceCount;
        long reused = resources.ReusedSurfaceCount;
        long captures = 0;
        for (int frame = 0; frame < 2048; frame++)
        {
            bool lower = frame % 2 == 0;
            Render(lower);
            captures += executor.Diagnostics.Counters.CaptureCount;
            Assert.InRange(executor.Diagnostics.Counters.PeakLiveSurfaceCount, 0,
                (lower ? lowPlan : highPlan).PeakLiveSurfaces);
            Assert.Equal(0, executor.Diagnostics.Count);
        }
        Assert.Equal(created, resources.CreatedSurfaceCount);
        Assert.True(resources.ReusedSurfaceCount > reused);
        Assert.Equal(graphPipeline, resources.GetPipeline(SdlGpuTextureFormat.R16G16B16A16Float));
        Assert.Equal(presentationPipeline, PresentationPipeline());
        // The unchanged capture dependency is retained even as layer opacity changes.
        Assert.Equal(0, captures);
        resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(resources.TotalBytes, resources.FreeBytes);
    }

    private static DrawCommandList Scene(string scenario, float opacity = 0.5f)
    {
        PrismLayerDefinition layer = scenario switch
        {
            "styles" => new(new(1), "Style stress", styles: Enumerable.Range(0, 48)
                .Select(_ => new PrismStyleDefinition(PrismStyleId.ColorOverlay)).ToArray()),
            "chained" => new(new(10), "Chained", filters:
                [new(PrismFilterId.GaussianBlur), new(PrismFilterId.Threshold), new(PrismFilterId.Invert)]),
            "nested" => new(new(20), "Outer", filters: [new(PrismFilterId.Maximum)]),
            _ => PrismTestData.Layer(1, "Half opacity", opacity: opacity)
        };
        PrismDrawScope outer = PrismTestData.Scope(PrismTestData.Composition("Execution budget", layer),
            ownerToken: 93001, bounds: new(0, 0, 16, 16));
        if (scenario != "nested") return PrismTestData.Commands(DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), Color.White), DrawCommand.EndPrism());
        PrismDrawScope inner = PrismTestData.Scope(PrismTestData.Composition("Inner",
            new PrismLayerDefinition(new(21), "Inner", filters:
                [new(PrismFilterId.GaussianBlur), new(PrismFilterId.Invert)])),
            ownerToken: 93002, bounds: new(2, 2, 12, 12));
        return PrismTestData.Commands(DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), Color.White), DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(new(2, 2, 12, 12), Color.White), DrawCommand.EndPrism(), DrawCommand.EndPrism());
    }
}

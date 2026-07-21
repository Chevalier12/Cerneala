using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismOperationalDiagnosticsTests
{
    private const string RuntimeIdentifier = "0xDEADBEEF";
    private const string FailureDetail =
        "Prism failure on GPU resource 0xDEADBEEF.";

    [Fact]
    public void FallbackDiagnosticsHaveStableCodesStagesAndRedactedDetails()
    {
        (PrismFallbackReason Reason, string Code, PrismExecutionDiagnosticStage Stage)[] cases =
        [
            (PrismFallbackReason.UnsupportedCapability, "PRISM7001", PrismExecutionDiagnosticStage.CapabilityCheck),
            (PrismFallbackReason.MissingKernel, "PRISM7002", PrismExecutionDiagnosticStage.KernelLookup),
            (PrismFallbackReason.MissingBackdrop, "PRISM7003", PrismExecutionDiagnosticStage.BackdropAcquisition),
            (PrismFallbackReason.MissingResource, "PRISM7004", PrismExecutionDiagnosticStage.ResourceResolution),
            (PrismFallbackReason.InvalidColorProfile, "PRISM7005", PrismExecutionDiagnosticStage.ColorProfile),
            (PrismFallbackReason.SurfaceAllocationFailed, "PRISM7006", PrismExecutionDiagnosticStage.SurfaceBudget),
            (PrismFallbackReason.ShaderUnavailable, "PRISM7007", PrismExecutionDiagnosticStage.ShaderLoad),
        ];
        PrismExecutionDiagnostics diagnostics = new(
            detailedDiagnosticsEnabled: true);

        for (int index = 0; index < cases.Length; index++)
        {
            (PrismFallbackReason reason, string code, PrismExecutionDiagnosticStage stage) = cases[index];
            PrismFallbackAction action = diagnostics.Record(
                nodeId: null,
                scopeIndex: index,
                reason,
                FailureDetail);
            PrismExecutionDiagnostic actual = diagnostics.Get(index);

            Assert.Equal(code, actual.Code);
            Assert.Equal(stage, actual.Stage);
            Assert.Equal(reason, actual.Reason);
            Assert.Equal(PrismFallbackPolicy.Resolve(reason), action);
            Assert.Equal(action, actual.Action);
            Assert.Equal(index, actual.ScopeIndex);
            Assert.Contains("<gpu-id>", actual.Detail, StringComparison.Ordinal);
            Assert.DoesNotContain(RuntimeIdentifier, actual.Detail, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(cases.Length, diagnostics.Count);
        Assert.Equal(cases.Length, diagnostics.DetailedCount);
        Assert.Equal("PRISM7007", diagnostics.LastFallback?.Code);
    }

    [Fact]
    public void ExecutedGraphDumpIsDeterministicAndRedactsRuntimeOwners()
    {
        string first = CaptureDump(ownerToken: 17);
        string second = CaptureDump(ownerToken: 9_001);

        Assert.Equal(first, second);
        Assert.StartsWith(
            "prism-execution v2 runtime-identifiers=redacted",
            first,
            StringComparison.Ordinal);
        Assert.Contains("owner=scope-0", first, StringComparison.Ordinal);
        Assert.Contains("composition=OperationalDiagnostics", first, StringComparison.Ordinal);
        Assert.Contains("code=PRISM7007 stage=ShaderLoad", first, StringComparison.Ordinal);
        Assert.Contains("<gpu-id>", first, StringComparison.Ordinal);
        Assert.DoesNotContain(RuntimeIdentifier, first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("owner=17", first, StringComparison.Ordinal);
        Assert.DoesNotContain("owner=9001", first, StringComparison.Ordinal);
    }

    [Fact]
    public void DetailedDiagnosticsDoNotRetainPrismInstances()
    {
        (PrismExecutionDiagnostics diagnostics, WeakReference<PrismInstance> instance) =
            CaptureWithoutRetainingInstance();

        ForceCollection();

        Assert.True(diagnostics.HasCapturedExecution);
        Assert.False(instance.TryGetTarget(out _));
        GC.KeepAlive(diagnostics);
    }

    [Fact]
    public void DisabledDiagnosticsAllocateNothingPerFrameAfterWarmup()
    {
        DiagnosticScene scene = CreateScene(ownerToken: 41);
        PrismExecutionDiagnostics diagnostics = new(
            detailedDiagnosticsEnabled: false);
        for (int index = 0; index < 2_048; index++)
        {
            RunDisabledFrame(diagnostics, scene.Analysis, scene.Plan);
        }

        ForceCollection();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 4_096; index++)
        {
            RunDisabledFrame(diagnostics, scene.Analysis, scene.Plan);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.False(diagnostics.DetailedDiagnosticsEnabled);
        Assert.False(diagnostics.HasCapturedExecution);
        Assert.Equal(0, diagnostics.DetailedCount);
        Assert.Null(diagnostics.LastFallback);
        Assert.Equal(1, diagnostics.Counters.FallbackCount);
        Assert.Throws<InvalidOperationException>(
            diagnostics.DumpExecutedGraph);
    }

    [Fact]
    public void OperationalViewCombinesExecutionBackdropAndMotionState()
    {
        DiagnosticScene scene = CreateScene(ownerToken: 55);
        PrismExecutionDiagnostics diagnostics = new();
        int plannedPassCount = ExpectedPassCount(scene.Plan);
        diagnostics.BeginExecution(
            scene.Analysis,
            scene.Plan,
            plannedPassCount);
        diagnostics.RecordGraphPass(
            scene.Plan.OptimizedGraph.GetNode(
                scene.Plan.ExecutionOrder[0]));
        diagnostics.ObserveLiveSurfaces(4);
        diagnostics.CompleteExecution(
            createdSurfaces: 5,
            reusedSurfaces: 7,
            activeSurfaces: 2,
            surfaceBytes: 1_024,
            peakSurfaceBytes: 2_048,
            submitTime: TimeSpan.FromMilliseconds(1));
        diagnostics.Record(
            nodeId: null,
            scopeIndex: 0,
            PrismFallbackReason.MissingBackdrop,
            "No compatible backdrop texture was supplied.");
        BackdropFrameFailureDiagnostic backdropFailure = new(
            "PRISM7101",
            BackdropFrameFailureReason.MissingSource,
            "No source is configured.");
        BackdropFrameDiagnosticSnapshot backdrop = new(
            RequestedFrames: 8,
            AcquiredFrames: 6,
            SharedScopeUses: 3,
            SkippedFrames: 1,
            FailedFrames: 1,
            backdropFailure);

        PrismOperationalDiagnostics snapshot =
            PrismOperationalDiagnostics.Capture(
                diagnostics,
                backdrop,
                activeBackdropLeaseCount: 2,
                motionActive: true);

        Assert.True(snapshot.DevelopmentDetailsEnabled);
        Assert.Equal(1, snapshot.ActiveCompositionCount);
        Assert.Equal(plannedPassCount, snapshot.PlannedPassCount);
        Assert.Equal(1, snapshot.ExecutedPassCount);
        Assert.Equal(4, snapshot.PeakLiveSurfaceCount);
        Assert.Equal(5, snapshot.SurfaceAllocationCount);
        Assert.Equal(7, snapshot.SurfaceReuseCount);
        Assert.Equal(1_024, snapshot.SurfaceByteCount);
        Assert.Equal(2_048, snapshot.PeakSurfaceByteCount);
        Assert.Equal(1, snapshot.FallbackCount);
        Assert.Equal("PRISM7003", snapshot.LastFallback?.Code);
        Assert.Equal(backdrop, snapshot.Backdrop);
        Assert.Equal(2, snapshot.ActiveBackdropLeaseCount);
        Assert.True(snapshot.MotionActive);
    }

    [Fact]
    public void SurfaceBudgetFailureReportsRequestedCurrentAndLimitBytes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismGraphExecutorTests.TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            PrismGraphExecutorTests.SurfaceWidth,
            PrismGraphExecutorTests.SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics,
            new PrismRendererOptions
            {
                SurfaceHardByteLimit = 0,
                RetainedCacheSoftByteLimit = 0,
                RetainedCacheEntryLimit = 0,
                EnableDevelopmentDiagnostics = true
            },
            retainedCacheEnabled: false);
        DiagnosticScene scene = CreateScene(ownerToken: 77);

        PrismGraphExecutorTests.ExecuteFrame(
            renderer,
            executor,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            new Viewport(
                0,
                0,
                PrismGraphExecutorTests.SurfaceWidth,
                PrismGraphExecutorTests.SurfaceHeight));

        PrismExecutionDiagnostic failure = Assert.Single(
            Enumerable.Range(0, diagnostics.DetailedCount)
                .Select(diagnostics.Get)
                .Where(entry =>
                    entry.Reason ==
                        PrismFallbackReason.SurfaceAllocationFailed));
        Assert.Equal("PRISM7006", failure.Code);
        Assert.Equal(
            PrismExecutionDiagnosticStage.SurfaceBudget,
            failure.Stage);
        Assert.Contains("requestedBytes=", failure.Detail, StringComparison.Ordinal);
        Assert.Contains("currentBytes=", failure.Detail, StringComparison.Ordinal);
        Assert.Contains("hardByteLimit=0", failure.Detail, StringComparison.Ordinal);
    }

    private static string CaptureDump(long ownerToken)
    {
        DiagnosticScene scene = CreateScene(ownerToken);
        PrismExecutionDiagnostics diagnostics = new();
        diagnostics.BeginExecution(
            scene.Analysis,
            scene.Plan,
            ExpectedPassCount(scene.Plan));
        for (int index = 0;
            index < scene.Plan.ExecutionOrder.Length;
            index++)
        {
            diagnostics.RecordGraphPass(
                scene.Plan.OptimizedGraph.GetNode(
                    scene.Plan.ExecutionOrder[index]));
        }
        diagnostics.Record(
            nodeId: null,
            scopeIndex: 0,
            PrismFallbackReason.ShaderUnavailable,
            FailureDetail);
        diagnostics.CompleteExecution(
            createdSurfaces: 2,
            reusedSurfaces: 3,
            activeSurfaces: 0,
            surfaceBytes: 0,
            peakSurfaceBytes: 2_048,
            submitTime: TimeSpan.FromTicks(3));
        return diagnostics.DumpExecutedGraph();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (
        PrismExecutionDiagnostics Diagnostics,
        WeakReference<PrismInstance> Instance)
        CaptureWithoutRetainingInstance()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Lifetime",
                PrismTestData.Layer(1, "Content"));
        PrismInstance instance = new(definition);
        WeakReference<PrismInstance> weak = new(instance);
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(81),
            new DrawRect(0, 0, 16, 16),
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 16, 16),
                Color.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        PrismExecutionDiagnostics diagnostics = new();
        diagnostics.BeginExecution(
            analysis,
            plan,
            ExpectedPassCount(plan));
        diagnostics.RecordGraphPass(
            plan.OptimizedGraph.GetNode(
                plan.ExecutionOrder[0]));
        return (diagnostics, weak);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunDisabledFrame(
        PrismExecutionDiagnostics diagnostics,
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan)
    {
        diagnostics.BeginExecution(
            analysis,
            plan,
            ExpectedPassCount(plan));
        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            diagnostics.RecordGraphPass(
                plan.OptimizedGraph.GetNode(
                    plan.ExecutionOrder[index]));
        }
        diagnostics.ObserveLiveSurfaces(2);
        diagnostics.Record(
            nodeId: null,
            scopeIndex: 0,
            PrismFallbackReason.UnsupportedCapability,
            "Unsupported capability.");
        diagnostics.CompleteExecution(
            createdSurfaces: 0,
            reusedSurfaces: 0,
            activeSurfaces: 0,
            surfaceBytes: 0,
            peakSurfaceBytes: 0,
            submitTime: TimeSpan.Zero);
    }

    private static DiagnosticScene CreateScene(long ownerToken)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "OperationalDiagnostics",
                PrismTestData.Layer(1, "Content")),
            ownerToken,
            new DrawRect(
                0,
                0,
                PrismGraphExecutorTests.SurfaceWidth,
                PrismGraphExecutorTests.SurfaceHeight));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(
                    0,
                    0,
                    PrismGraphExecutorTests.SurfaceWidth,
                    PrismGraphExecutorTests.SurfaceHeight),
                Color.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new DiagnosticScene(commands, analysis, plan);
    }

    private static int ExpectedPassCount(
        PrismGraphExecutionPlan plan) =>
        checked(
            plan.ExecutionOrder.Length +
            plan.OptimizedGraph.Scopes.Length);

    private static void ForceCollection()
    {
        for (int index = 0; index < 3; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed record DiagnosticScene(
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan);
}

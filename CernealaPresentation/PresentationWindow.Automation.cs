using System.Diagnostics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Text;

namespace Cerneala.Presentation;

public partial class PresentationWindow
{
    private async Task RunAutomationIfRequestedAsync()
    {
        string? frameBudgetReportPath =
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT");
        if (!string.IsNullOrWhiteSpace(frameBudgetReportPath))
        {
            await RunAutomationWithErrorReportAsync(
                frameBudgetReportPath,
                () => ExecuteFrameBudgetAutomationAsync(frameBudgetReportPath));
            return;
        }

        string? reportPath = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return;
        }

        await RunAutomationWithErrorReportAsync(reportPath, () => ExecuteAutomationAsync(reportPath));
    }

    private async Task RunAutomationWithErrorReportAsync(string reportPath, Func<Task> automation)
    {
        try
        {
            await automation();
        }
        catch (Exception exception)
        {
            string errorPath = Path.GetFullPath(reportPath) + ".error.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Close();
        }
    }

    private async Task ExecuteFrameBudgetAutomationAsync(string reportPath)
    {
        int cycles = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_FRAME_BUDGET_CYCLES",
            defaultValue: 8,
            minimum: 1,
            maximum: 100);
        int framesPerLoad = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_FRAME_BUDGET_FRAMES_PER_LOAD",
            defaultValue: 45,
            minimum: 1,
            maximum: 1_000);
        List<FrameBudgetSample> samples = new(cycles * framesPerLoad * (ChapterNames.Length - 1));
        ButtonAutomationPeer next = new(NextButton);
        Stopwatch runTime = Stopwatch.StartNew();

        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            for (int chapterIndex = 1; chapterIndex < ChapterNames.Length; chapterIndex++)
            {
                while (currentChapter != chapterIndex - 1)
                {
                    await InvokeNextAndWaitForFrameAsync(next);
                }

                await CaptureFrameBudgetLoadAsync(
                    next,
                    cycle,
                    chapterIndex,
                    framesPerLoad,
                    runTime,
                    samples);
            }
        }

        FrameBudgetReport report = new(
            SchemaVersion: 1,
            StartedUtc: DateTimeOffset.UtcNow - runTime.Elapsed,
            Cycles: cycles,
            FramesPerLoad: framesPerLoad,
            Samples: samples);
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }

    private async Task CaptureFrameBudgetLoadAsync(
        ButtonAutomationPeer next,
        int cycle,
        int chapterIndex,
        int framesPerLoad,
        Stopwatch runTime,
        List<FrameBudgetSample> samples)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int frameIndex = 0;
        int previousGen0Collections = GC.CollectionCount(0);
        int previousGen1Collections = GC.CollectionCount(1);
        int previousGen2Collections = GC.CollectionCount(2);
        long previousAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            UiFrame frame = LastFrame ??
                throw new InvalidOperationException("FrameRendered was raised without a frame.");
            int gen0Collections = GC.CollectionCount(0);
            int gen1Collections = GC.CollectionCount(1);
            int gen2Collections = GC.CollectionCount(2);
            long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
            samples.Add(new FrameBudgetSample(
                cycle,
                ChapterNames[chapterIndex],
                chapterIndex,
                frameIndex,
                frame.ProcessingTime.TotalMilliseconds,
                frame.ElapsedTime.TotalMilliseconds,
                frame.Stats,
                FrameBudgetTimingSample.From(frame.DiagnosticsTiming),
                gen0Collections - previousGen0Collections,
                gen1Collections - previousGen1Collections,
                gen2Collections - previousGen2Collections,
                allocatedBytes - previousAllocatedBytes,
                cycle == 1,
                runTime.Elapsed.TotalMilliseconds));
            previousGen0Collections = gen0Collections;
            previousGen1Collections = gen1Collections;
            previousGen2Collections = gen2Collections;
            previousAllocatedBytes = allocatedBytes;
            frameIndex++;

            if (frameIndex >= framesPerLoad)
            {
                FrameRendered -= handler;
                completion.TrySetResult();
                return;
            }

            Invalidate(InvalidationFlags.Render, "frame budget automation sample");
        };

        FrameRendered += handler;
        if (!next.Invoke())
        {
            FrameRendered -= handler;
            throw new InvalidOperationException("Frame-budget automation could not invoke the Next button.");
        }

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private async Task InvokeNextAndWaitForFrameAsync(ButtonAutomationPeer next)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            completion.TrySetResult();
        };

        FrameRendered += handler;
        if (!next.Invoke())
        {
            FrameRendered -= handler;
            throw new InvalidOperationException("Frame-budget automation could not navigate to the next chapter.");
        }

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static int ReadBoundedEnvironmentInteger(
        string variableName,
        int defaultValue,
        int minimum,
        int maximum)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(variableName), out int requestedValue)
            ? Math.Clamp(requestedValue, minimum, maximum)
            : defaultValue;
    }

    private PrismOperationalDiagnostics? CapturePrismDiagnosticsSnapshot()
    {
        return WindowApplicationRuntime.Current?.CapturePrismDiagnostics(this);
    }

    private async Task ExecuteAutomationAsync(string reportPath)
    {
        int cycles = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_CYCLES"),
            out int requestedCycles)
            ? Math.Clamp(requestedCycles, 1, 100)
            : 10;
        List<AutomationSample> samples = [];
        ButtonAutomationPeer next = new(NextButton);

        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
        samples.Add(CaptureAutomationSample(0, "baseline"));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            for (int click = 0; click < ChapterNames.Length; click++)
            {
                if (!next.Invoke())
                {
                    throw new InvalidOperationException("Presentation automation could not invoke the Next button.");
                }

                TimeSpan maximumWait = currentChapter == 5
                    ? TimeSpan.FromSeconds(5)
                    : TimeSpan.FromSeconds(2);
                await WaitForFrameIdleAsync(maximumWait);
                samples.Add(CaptureAutomationSample(cycle, ChapterNames[currentChapter]));
            }
        }

        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }

    private async Task WaitForFrameIdleAsync(TimeSpan maximumWait)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        int stableChecks = 0;
        while (stableChecks < 4)
        {
            await Task.Delay(25);
            bool hasWork = LastFrame?.Stats.HasWork != false;
            stableChecks = hasWork ? 0 : stableChecks + 1;
            if (timeout.Elapsed > maximumWait)
            {
                return;
            }
        }
    }

    private AutomationSample CaptureAutomationSample(int cycle, string chapter)
    {
        CollectAutomationGarbage();

        using Process process = Process.GetCurrentProcess();
        PrismOperationalDiagnostics? prism = CapturePrismDiagnosticsSnapshot();
        return new AutomationSample(
            cycle,
            chapter,
            GC.GetTotalMemory(forceFullCollection: false),
            process.PrivateMemorySize64,
            process.WorkingSet64,
            TextMeasurer.Default.LayoutCache.Count,
            Root?.Trace.Entries.Count ?? 0,
            Root?.RetainedRenderCache.RootCommands.Count ?? 0,
            prism);
    }

    private static void CollectAutomationGarbage()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private sealed record AutomationSample(
        int Cycle,
        string Chapter,
        long ManagedBytes,
        long PrivateBytes,
        long WorkingSetBytes,
        int TextLayoutCacheCount,
        int InvalidationTraceCount,
        int RootCommandCount,
        PrismOperationalDiagnostics? Prism);

    private sealed record FrameBudgetReport(
        int SchemaVersion,
        DateTimeOffset StartedUtc,
        int Cycles,
        int FramesPerLoad,
        IReadOnlyList<FrameBudgetSample> Samples);

    private readonly record struct FrameBudgetSample(
        int Cycle,
        string Chapter,
        int ChapterIndex,
        int FrameIndex,
        double ProcessingTimeMs,
        double ElapsedTimeMs,
        FrameStats FrameStats,
        FrameBudgetTimingSample Timing,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        long AllocatedBytes,
        bool IsCold,
        double TimestampMs);

    private readonly record struct FrameBudgetTimingSample(
        double InputCollectionMs,
        double RetainedUpdateMs,
        double UpdatePreparationMs,
        double ScheduledProcessingMs,
        double InputDispatchMs,
        double InputProcessingMs,
        double RetainedCommitMs,
        double CursorPublicationMs,
        double ScheduledInheritedMs,
        double ScheduledCommandStateMs,
        double ScheduledAspectMs,
        double ScheduledMeasureMs,
        double ScheduledArrangeMs,
        double ScheduledRenderMs,
        double ScheduledHitTestMs,
        double ScheduledMotionMs,
        double BeginFrameMs,
        double DrawingMs,
        double DrawingPreparationMs,
        double TextRequestCollectionMs,
        double TextRasterizationMs,
        double TextAtlasUploadMs,
        double CommandRenderingMs,
        double DrawingCleanupMs,
        int TextRequestCount,
        long RasterizedPixelCount)
    {
        public static FrameBudgetTimingSample From(UiFrameTiming timing)
        {
            DrawingBackendFrameTiming drawing = timing.DrawingBackend;
            FramePhaseTiming scheduled = timing.ScheduledPhases;
            return new FrameBudgetTimingSample(
                timing.InputCollection.TotalMilliseconds,
                timing.RetainedUpdate.TotalMilliseconds,
                timing.UpdatePreparation.TotalMilliseconds,
                timing.ScheduledProcessing.TotalMilliseconds,
                timing.InputDispatch.TotalMilliseconds,
                timing.InputProcessing.TotalMilliseconds,
                timing.RetainedCommit.TotalMilliseconds,
                timing.CursorPublication.TotalMilliseconds,
                scheduled.InheritedProperties.TotalMilliseconds,
                scheduled.CommandState.TotalMilliseconds,
                scheduled.Aspect.TotalMilliseconds,
                scheduled.Measure.TotalMilliseconds,
                scheduled.Arrange.TotalMilliseconds,
                scheduled.Render.TotalMilliseconds,
                scheduled.HitTest.TotalMilliseconds,
                scheduled.Motion.TotalMilliseconds,
                timing.BeginFrame.TotalMilliseconds,
                timing.Drawing.TotalMilliseconds,
                drawing.Preparation.TotalMilliseconds,
                drawing.TextRequestCollection.TotalMilliseconds,
                drawing.TextRasterization.TotalMilliseconds,
                drawing.TextAtlasUpload.TotalMilliseconds,
                drawing.CommandRendering.TotalMilliseconds,
                drawing.Cleanup.TotalMilliseconds,
                drawing.TextRequestCount,
                drawing.RasterizedPixelCount);
        }
    }
}

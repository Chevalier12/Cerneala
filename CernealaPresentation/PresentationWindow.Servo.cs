using System.Diagnostics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Detective;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Servo;
using Cerneala.UI.Text;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Presentation;

public partial class PresentationWindow
{
    private async Task RunServoIfRequestedAsync()
    {
        string? outerGlowLabReportPath =
            Environment.GetEnvironmentVariable("CERNEALA_PRISM_OUTER_GLOW_LAB_REPORT");
        if (!string.IsNullOrWhiteSpace(outerGlowLabReportPath))
        {
            await RunServoWithErrorReportAsync(
                outerGlowLabReportPath,
                () => ExecuteOuterGlowLabAsync(outerGlowLabReportPath));
            return;
        }

        string? frameBudgetReportPath =
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT");
        if (!string.IsNullOrWhiteSpace(frameBudgetReportPath))
        {
            await RunServoWithErrorReportAsync(
                frameBudgetReportPath,
                () => ExecuteFrameBudgetServoAsync(frameBudgetReportPath));
            return;
        }

        string? reportPath = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return;
        }

        await RunServoWithErrorReportAsync(reportPath, () => ExecuteServoAsync(reportPath));
    }

    private async Task RunServoWithErrorReportAsync(string reportPath, Func<Task> servoWork)
    {
        try
        {
            await servoWork();
        }
        catch (Exception exception)
        {
            string errorPath = Path.GetFullPath(reportPath) + ".error.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Close();
        }
    }

    private async Task ExecuteFrameBudgetServoAsync(string reportPath)
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
        List<FrameBudgetSample> samples = new(cycles * framesPerLoad * (ChapterOrder.Length - 1));
        Stopwatch runTime = Stopwatch.StartNew();

        await WaitForServoIdleAsync(TimeSpan.FromSeconds(2));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            foreach (PresentationChapter chapter in ChapterOrder.Skip(1))
            {
                PresentationChapter previous = ChapterOrder[(ChapterIndex(chapter) - 1 + ChapterOrder.Length) % ChapterOrder.Length];
                while (currentChapter != previous)
                {
                    await ClickNextChapterAsync();
                }

                await CaptureFrameBudgetLoadAsync(
                    cycle,
                    chapter,
                    framesPerLoad,
                    runTime,
                    samples);
            }
        }

        FrameBudgetReport report = new(
            SchemaVersion: 2,
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
        int cycle,
        PresentationChapter chapter,
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
                ChapterName(chapter),
                ChapterIndex(chapter),
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

            Invalidate(InvalidationFlags.Render, "frame budget Servo sample");
        };

        FrameRendered += handler;
        try
        {
            await ClickNextChapterAsync();
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch
        {
            FrameRendered -= handler;
            throw;
        }
    }

    private async Task ClickNextChapterAsync()
    {
        int currentIndex = ChapterIndex(currentChapter);
        PresentationChapter expectedChapter = ChapterOrder[
            currentIndex == ChapterOrder.Length - 1 ? 0 : currentIndex + 1];
        await PresentationServo.ClickAsync(ServoTarget.ById(PresentationNextServoId));
        await WaitForChapterAsync(expectedChapter);
    }

    private async Task ClickChapterNavigationAsync(PresentationChapter chapter)
    {
        await PresentationServo.ClickAsync(ServoTarget.ById(ChapterNavigationServoId(chapter)));
        await WaitForChapterAsync(chapter);
    }

    private Task WaitForChapterAsync(PresentationChapter chapter) =>
        PresentationServo.WaitForAsync(
            ServoTarget.ById(PresentationChapterTitleServoId).WithName(ChapterName(chapter)),
            ServoCondition.Visible);

    private async Task WaitForServoIdleAsync(TimeSpan maximumWait)
    {
        ServoApi boundedServo = new(this, new ServoOptions { DefaultTimeout = maximumWait });
        try
        {
            await boundedServo.WaitForIdleAsync();
        }
        catch (ServoTimeoutException)
        {
            // Presentation contains intentionally continuous Motion scenes. Preserve the
            // existing bounded best-effort settle contract without reporting false idle.
        }
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

    private async Task ExecuteServoAsync(string reportPath)
    {
        int cycles = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_CYCLES"),
            out int requestedCycles)
            ? Math.Clamp(requestedCycles, 1, 100)
            : 10;
        List<ServoSample> samples = [];
        await WaitForServoIdleAsync(TimeSpan.FromSeconds(2));
        samples.Add(CaptureServoSample(0, "baseline"));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            for (int click = 0; click < ChapterOrder.Length; click++)
            {
                await ClickNextChapterAsync();

                TimeSpan maximumWait = currentChapter is PresentationChapter.Motion or PresentationChapter.Prism
                    ? TimeSpan.FromSeconds(5)
                    : TimeSpan.FromSeconds(2);
                await WaitForServoIdleAsync(maximumWait);
                samples.Add(CaptureServoSample(cycle, ChapterName(currentChapter)));
            }
        }

        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }

    private ServoSample CaptureServoSample(int cycle, string chapter)
    {
        CollectServoGarbage();

        using Process process = Process.GetCurrentProcess();
        PrismOperationalDiagnostics? prism = CapturePrismDiagnosticsSnapshot();
        Detective? detective = Root?.Detective;
        RootRenderDiagnosticsSnapshot? rendering = detective?.CaptureRendering();
        return new ServoSample(
            cycle,
            chapter,
            GC.GetTotalMemory(forceFullCollection: false),
            process.PrivateMemorySize64,
            process.WorkingSet64,
            TextMeasurer.Default.LayoutCache.Count,
            detective?.Invalidation.Entries.Count ?? 0,
            rendering?.RootCommandCount ?? 0,
            prism);
    }

    private static void CollectServoGarbage()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private sealed record ServoSample(
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
        double CompleteFrameMs,
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
                timing.CompleteFrame.TotalMilliseconds,
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

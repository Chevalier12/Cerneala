using System.Diagnostics;
using System.Text.Json;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Text;

namespace Cerneala.Presentation;

public partial class PresentationWindow
{
    private async Task RunAutomationIfRequestedAsync()
    {
        string? reportPath = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return;
        }

        try
        {
            await ExecuteAutomationAsync(reportPath);
        }
        catch (Exception exception)
        {
            string errorPath = Path.GetFullPath(reportPath) + ".error.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Close();
        }
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
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        using Process process = Process.GetCurrentProcess();
        return new AutomationSample(
            cycle,
            chapter,
            GC.GetTotalMemory(forceFullCollection: false),
            process.PrivateMemorySize64,
            process.WorkingSet64,
            TextMeasurer.Default.LayoutCache.Count,
            Root?.Trace.Entries.Count ?? 0,
            Root?.RetainedRenderCache.RootCommands.Count ?? 0);
    }

    private sealed record AutomationSample(
        int Cycle,
        string Chapter,
        long ManagedBytes,
        long PrivateBytes,
        long WorkingSetBytes,
        int TextLayoutCacheCount,
        int InvalidationTraceCount,
        int RootCommandCount);
}

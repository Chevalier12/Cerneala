using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Presentation;

public partial class PresentationWindow : Window
{
    private static readonly string[] ChapterNames =
    [
        "WELCOME",
        "RETAINED MODEL",
        "BUILD-TIME MARKUP",
        "ASPECT DESIGN SYSTEM",
        "MOTION",
        "FRAME PIPELINE",
        "DIAGNOSTICS"
    ];

    private int currentChapter;
    private bool contentReady;
    private bool skipNextDiagnosticsRefresh;
    private int motionReplayVersion;
    private ToggleButton[] tourNavigation = [];

    internal event EventHandler? ReactorResetRequested;
    internal event EventHandler? ReactorIgnitionRequested;
    internal event EventHandler? ReactorTrajectoryRequested;
    internal event EventHandler? ReactorCommitRequested;
    internal event EventHandler? ReactorCancelRequested;
    internal event EventHandler? PipelineRequested;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (contentReady)
        {
            return;
        }

        contentReady = true;
        InitializeTourNavigation();
        int initialChapter = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_START_CHAPTER"),
            out int requestedChapter)
            ? Math.Clamp(requestedChapter - 1, 0, ChapterNames.Length - 1)
            : 0;
        ShowChapter(initialChapter);
        _ = CaptureIfRequestedAsync();
        _ = RunAutomationIfRequestedAsync();
    }

    private void InitializeTourNavigation()
    {
        tourNavigation = [NavWelcome, NavRetained, NavMarkup, NavAspect, NavMotion, NavPipeline, NavDiagnostics];
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null || currentChapter != 6)
        {
            return;
        }

        if (skipNextDiagnosticsRefresh)
        {
            skipNextDiagnosticsRefresh = false;
            return;
        }

        DiagFrameTime.Text = $"{LastFrame.ElapsedTime.TotalMilliseconds:0.00} ms";
        DiagLayout.Text = $"{LastFrame.Stats.MeasuredElements} / {LastFrame.Stats.ArrangedElements}";
        DiagRender.Text = $"{LastFrame.Stats.RenderedElements} / {LastFrame.Stats.HitTestElements}";
        DiagSummary.Text = LastFrame.Stats.HasWork ? "dirty work committed" : "idle fast path";
        skipNextDiagnosticsRefresh = true;
    }

    private void OnWelcome(UiElementId sender, RoutedEventArgs args) => ShowChapter(0);
    private void OnRetained(UiElementId sender, RoutedEventArgs args) => ShowChapter(1);
    private void OnMarkup(UiElementId sender, RoutedEventArgs args) => ShowChapter(2);
    private void OnAspect(UiElementId sender, RoutedEventArgs args) => ShowChapter(3);
    private void OnMotion(UiElementId sender, RoutedEventArgs args) => ShowChapter(4);
    private void OnPipeline(UiElementId sender, RoutedEventArgs args) => ShowChapter(5);
    private void OnDiagnostics(UiElementId sender, RoutedEventArgs args) => ShowChapter(6);

    private void OnPrevious(UiElementId sender, RoutedEventArgs args)
    {
        ShowChapter(Math.Max(0, currentChapter - 1));
    }

    private void OnNext(UiElementId sender, RoutedEventArgs args)
    {
        ShowChapter(currentChapter == ChapterNames.Length - 1 ? 0 : currentChapter + 1);
    }

    private void ShowChapter(int index)
    {
        UIElement[] pages = [PageWelcome, PageRetained, PageMarkup, PageAspect, PageMotion, PagePipeline, PageDiagnostics];
        int nextChapter = Math.Clamp(index, 0, pages.Length - 1);
        if (currentChapter == 4 && nextChapter != 4)
        {
            motionReplayVersion++;
            ReactorCancelRequested?.Invoke(this, EventArgs.Empty);
        }

        currentChapter = nextChapter;
        for (int i = 0; i < pages.Length; i++)
        {
            bool selected = i == currentChapter;
            pages[i].Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            tourNavigation[i].IsChecked = selected;
        }

        HeaderChapterText.Text = ChapterNames[currentChapter];
        ChapterCounter.Text = $"CHAPTER {currentChapter + 1:00} / {ChapterNames.Length:00}";
        PreviousButton.IsEnabled = currentChapter > 0;
        NextButton.Content = currentChapter == ChapterNames.Length - 1 ? "RESTART TOUR  ->" : "NEXT  ->";

        if (currentChapter == 4)
        {
            ReplayMotion();
        }
        else if (currentChapter == 5)
        {
            RunPipeline();
        }
    }

    private void OnReplayMotion(UiElementId sender, RoutedEventArgs args) => ReplayMotion();

    private void ReplayMotion()
    {
        int version = ++motionReplayVersion;
        _ = ReplayMotionAsync(version);
    }

    private async Task ReplayMotionAsync(int version)
    {
        MotionStatusText.Text = "ARMING / 00 NODES";
        MotionClockText.Text = "T + 00:00.000";
        MotionTransactionText.Text = "OPEN";
        MotionPhaseIndex.Text = "00";
        MotionFieldReadout.Text = "FIELD LOCKED";
        MotionReactorState.Text = "STANDBY";
        ReactorResetRequested?.Invoke(this, EventArgs.Empty);
        if (!await ContinueReplayAfterAsync(version, 120)) return;

        MotionPhaseIndex.Text = "01";
        MotionStatusText.Text = "IGNITION / 04 NODES";
        MotionReactorState.Text = "CLOCK PRIMED";
        ReactorIgnitionRequested?.Invoke(this, EventArgs.Empty);
        if (!await ContinueReplayAfterAsync(version, 650)) return;

        MotionPhaseIndex.Text = "02";
        MotionClockText.Text = "T + 00:00.620";
        MotionStatusText.Text = "TRAJECTORIES / 15 NODES";
        MotionFieldReadout.Text = "FIELD IN MOTION";
        MotionReactorState.Text = "COMPOSING";
        ReactorTrajectoryRequested?.Invoke(this, EventArgs.Empty);
        if (!await ContinueReplayAfterAsync(version, 1_780)) return;

        MotionPhaseIndex.Text = "03";
        MotionClockText.Text = "T + 00:02.400";
        MotionStatusText.Text = "COMMITTING / 06 NODES";
        MotionTransactionText.Text = "COMMITTING";
        MotionReactorState.Text = "SETTLING";
        ReactorCommitRequested?.Invoke(this, EventArgs.Empty);
        if (!await ContinueReplayAfterAsync(version, 600)) return;

        MotionClockText.Text = "T + 00:02.920";
        MotionStatusText.Text = "SETTLED / GRAPH IDLE";
        MotionTransactionText.Text = "COMMITTED";
        MotionFieldReadout.Text = "FIELD STABLE";
        MotionReactorState.Text = "AT REST";
    }

    private async Task<bool> ContinueReplayAfterAsync(int version, int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);
        return version == motionReplayVersion && currentChapter == 4;
    }

    private void OnRunPipeline(UiElementId sender, RoutedEventArgs args) => RunPipeline();

    private void RunPipeline() => PipelineRequested?.Invoke(this, EventArgs.Empty);

    private void OnOpenMotionLab(UiElementId sender, RoutedEventArgs args)
    {
        MotionLabWindow lab = new() { Owner = this };
        lab.Show();
    }

    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TOUR_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (int.TryParse(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_HOVER_CHAPTER"),
                out int hoverChapter) &&
            hoverChapter >= 1 &&
            hoverChapter <= tourNavigation.Length)
        {
            tourNavigation[hoverChapter - 1].IsPointerOver = true;
        }

        bool captureDuringMotion = string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        if (captureDuringMotion)
        {
            await Task.Delay(1_350);
        }
        else
        {
            await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100);
        }
        string fullPath = Path.GetFullPath(path);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            SaveScreenshot(fullPath);
            File.WriteAllLines(Path.ChangeExtension(fullPath, ".metrics.txt"),
            [
                $"Chapter={currentChapter + 1}",
                $"RootCommands={Root?.RetainedRenderCache.RootCommands.Count ?? 0}",
                $"RenderCacheVersion={Root?.RetainedRenderCache.Version ?? 0}"
            ]);
            completion.TrySetResult();
        };

        FrameRendered += handler;
        Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
        await completion.Task;
    }
}

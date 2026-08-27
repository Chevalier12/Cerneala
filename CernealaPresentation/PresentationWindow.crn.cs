using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Automation;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.Presentation;

internal enum PresentationChapter
{
    Welcome,
    RetainedModel,
    Markup,
    Aspect,
    Motion,
    Prism,
    FramePipeline
}

public partial class PresentationWindow : Window
{
    private static readonly PresentationChapter[] ChapterOrder =
    [
        PresentationChapter.Welcome,
        PresentationChapter.RetainedModel,
        PresentationChapter.Markup,
        PresentationChapter.Aspect,
        PresentationChapter.Motion,
        PresentationChapter.Prism,
        PresentationChapter.FramePipeline
    ];

    private PresentationChapter currentChapter = PresentationChapter.Welcome;
    private bool contentReady;
    private bool tourTransitionStarted;
    private bool tourEntered;
    private bool aspectPagePrewarming;
    private bool aspectPagePreparedForNavigation;
    private Task? aspectPagePrewarmTask;
    private bool skipNextHeaderDiagnosticsRefresh;
    private bool suppressLiveDiagnostics;
    private bool outerGlowLabActive;
    private string[] conformancePrismSamples = [];
    private IReadOnlyDictionary<PresentationChapter, ToggleButton> tourNavigation =
        new Dictionary<PresentationChapter, ToggleButton>();
    private IReadOnlyDictionary<PresentationChapter, UIElement> tourPages =
        new Dictionary<PresentationChapter, UIElement>();

    private void OnContentRendered(object? sender, EventArgs args)
    {
        EnsureContentReady();
        OpeningSurface.Start();
        _ = PrewarmAspectPageAsync();
    }

    private void EnsureContentReady()
    {
        if (contentReady)
        {
            return;
        }

        contentReady = true;
        ApplyRequestedWindowSize();
        AutomationProperties.SetAutomationId(OpeningSurface, "presentation-opening");
        AutomationProperties.SetAutomationId(TourSurface, "presentation-tour");
        InitializeTourNavigation();
        int initialChapter = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_START_CHAPTER"),
            out int requestedChapter)
            ? Math.Clamp(requestedChapter - 1, 0, ChapterOrder.Length - 1)
            : 0;
        ShowChapter(ChapterOrder[initialChapter]);
        PageAspect.PrepareEditor();
    }

    private void OnOpeningContinue(object? sender, EventArgs args)
    {
        if (tourTransitionStarted)
        {
            return;
        }

        tourTransitionStarted = true;
        EnsureContentReady();
        _ = EnterTourAsync();
    }

    private async Task EnterTourAsync()
    {
        float transitionWidth = Math.Max(Width, ArrangedBounds.Width);
        SpringSpec<float> transitionSpring = new(stiffness: 420f, damping: 28f);
        TimeSpan transitionCaptureDelay = TimeSpan.FromMilliseconds(130);
        await PrewarmAspectPageAsync();
        TourSurface.Visibility = Visibility.Visible;

        using (IDisposable transitionSession = GeneratedMarkup.AttachMotionSession(this))
        using (MotionHandle openingMotion = GeneratedMarkup.StartMotionProperty(
                   transitionSession,
                   OpeningSurface,
                   UIElement.TranslateXProperty,
                   hasFrom: true,
                   from: 0f,
                   toCurrent: false,
                   to: -transitionWidth,
                   spec: transitionSpring,
                   new MotionPropertyStartOptions
                   {
                       HoldOnComplete = true,
                       DebugName = "Presentation opening exit"
                   }))
        using (MotionHandle tourMotion = GeneratedMarkup.StartMotionProperty(
                   transitionSession,
                   TourSurface,
                   UIElement.TranslateXProperty,
                   hasFrom: true,
                   from: transitionWidth,
                   toCurrent: false,
                   to: 0f,
                   spec: transitionSpring,
                   new MotionPropertyStartOptions
                   {
                       HoldOnComplete = true,
                       DebugName = "Presentation tour entrance"
                   }))
        {
            await Task.WhenAll(
                openingMotion.Completion.AsTask(),
                tourMotion.Completion.AsTask(),
                CaptureTransitionIfRequestedAsync(transitionCaptureDelay));
        }

        OpeningSurface.Visibility = Visibility.Collapsed;
        Title = "Cerneala / Inside the Frame";
        tourEntered = true;
        await RunRequestedWorkAsync();
    }

    private Task PrewarmAspectPageAsync()
    {
        return aspectPagePrewarmTask ??= PrewarmAspectPageCoreAsync();
    }

    private async Task PrewarmAspectPageCoreAsync()
    {
        if (currentChapter == PresentationChapter.Aspect || aspectPagePreparedForNavigation)
        {
            return;
        }

        aspectPagePrewarming = true;
        TourSurface.Visibility = Visibility.Visible;
        PageAspect.Visibility = Visibility.Hidden;
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            completion.TrySetResult();
        };

        FrameRendered += handler;
        Invalidate(
            Cerneala.UI.Invalidation.InvalidationFlags.Measure |
            Cerneala.UI.Invalidation.InvalidationFlags.Arrange |
            Cerneala.UI.Invalidation.InvalidationFlags.Render,
            "prewarm Aspect chapter");
        try
        {
            PageAspect.Visibility = Visibility.Visible;
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
            aspectPagePreparedForNavigation = true;
        }
        finally
        {
            FrameRendered -= handler;
            aspectPagePrewarming = false;
            PageAspect.Visibility = currentChapter == PresentationChapter.Aspect
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }

    private async Task CaptureTransitionIfRequestedAsync(TimeSpan delay)
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TRANSITION_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Task.Delay(delay);
        await CaptureScreenshotFrameAsync(
            Path.GetFullPath(path),
            () => Invalidate(
                Cerneala.UI.Invalidation.InvalidationFlags.Render,
                "presentation transition screenshot"));
    }

    internal void ApplyRequestedWindowSize()
    {
        Width = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_WIDTH",
            (int)Width,
            (int)MinWidth,
            7680);
        Height = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_HEIGHT",
            (int)Height,
            (int)MinHeight,
            4320);
    }

    private async Task RunRequestedWorkAsync()
    {
        await CaptureIfRequestedAsync();
        await RunAutomationIfRequestedAsync();
    }

    private void InitializeTourNavigation()
    {
        tourNavigation = new Dictionary<PresentationChapter, ToggleButton>
        {
            [PresentationChapter.Welcome] = NavWelcome,
            [PresentationChapter.RetainedModel] = NavRetained,
            [PresentationChapter.Markup] = NavMarkup,
            [PresentationChapter.Aspect] = NavAspect,
            [PresentationChapter.Motion] = NavMotion,
            [PresentationChapter.Prism] = NavPrism,
            [PresentationChapter.FramePipeline] = NavPipeline
        };
        tourPages = new Dictionary<PresentationChapter, UIElement>
        {
            [PresentationChapter.Welcome] = PageWelcome,
            [PresentationChapter.RetainedModel] = PageRetained,
            [PresentationChapter.Markup] = PageMarkup,
            [PresentationChapter.Aspect] = PageAspect,
            [PresentationChapter.Motion] = PageMotion,
            [PresentationChapter.Prism] = PagePrism,
            [PresentationChapter.FramePipeline] = PagePipeline
        };
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null || outerGlowLabActive || !tourEntered)
        {
            return;
        }

        if (!suppressLiveDiagnostics)
        {
            UpdateHeaderDiagnostics(LastFrame);
        }
        if (currentChapter == PresentationChapter.Prism && !suppressLiveDiagnostics)
        {
            PagePrism.UpdateDiagnostics(CapturePrismDiagnosticsSnapshot());
        }
        if (currentChapter == PresentationChapter.Aspect && !suppressLiveDiagnostics)
        {
            PageAspect.UpdateDiagnostics();
        }
    }

    private void UpdateHeaderDiagnostics(UiFrame frame)
    {
        if (skipNextHeaderDiagnosticsRefresh)
        {
            skipNextHeaderDiagnosticsRefresh = false;
            return;
        }

        HeaderDiagFrame.Text =
            $"{frame.ProcessingTime.TotalMilliseconds:0.00} ms\n" +
            (frame.Stats.HasWork ? "WORK COMMITTED" : "IDLE FAST PATH");
        HeaderDiagPhases.Text =
            $"INHERITED {frame.Stats.InheritedElements}  COMMAND {frame.Stats.CommandStateElements}\n" +
            $"ASPECT {frame.Stats.AspectElements}";
        HeaderDiagLayout.Text =
            $"QUEUED {frame.Stats.MeasuredElements} / {frame.Stats.ArrangedElements}\n" +
            $"CALLS {frame.Stats.MeasureCalls} / {frame.Stats.ArrangeCalls}";
        HeaderDiagRender.Text =
            $"RENDER {frame.Stats.RenderedElements}  HIT {frame.Stats.HitTestElements}\n" +
            $"REUSED {frame.Stats.ReusedCaches}  NO-WORK {frame.Stats.NoWorkFrames}";
        HeaderDiagMotion.Text =
            $"FRAME {frame.Stats.MotionFrames}  SAMPLE {frame.Stats.MotionNodesSampled}  " +
            $"VALUE {frame.Stats.MotionValuesChanged}  WRITE {frame.Stats.MotionPropertyWrites}\n" +
            $"DONE {frame.Stats.MotionCompleted}  R-INV {frame.Stats.MotionRenderInvalidations}  " +
            $"L-INV {frame.Stats.MotionLayoutInvalidations}  REDUCED {frame.Stats.MotionSkippedByReducedMotion}";
        HeaderDiagRelay.Text =
            $"SNAP {frame.Stats.RelaySnapshotCallbacks}  DEQ {frame.Stats.RelayDequeuedCallbacks}  " +
            $"EXEC {frame.Stats.RelayExecutedCallbacks}  BACK {frame.Stats.RelayBacklog}\n" +
            $"CANCEL {frame.Stats.RelayCanceledCallbacks}  FAULT {frame.Stats.RelayFaultedCallbacks}  " +
            $"DEFER {frame.Stats.RelayDeferredCallbacks}";
        skipNextHeaderDiagnosticsRefresh = true;
    }

    private void OnWelcome(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Welcome);
    private void OnRetained(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.RetainedModel);
    private void OnMarkup(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Markup);
    private void OnAspect(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Aspect);
    private void OnMotion(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Motion);
    private void OnPrism(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Prism);
    private void OnPipeline(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.FramePipeline);

    private void OnPrevious(UiElementId sender, RoutedEventArgs args)
    {
        int index = ChapterIndex(currentChapter);
        ShowChapter(ChapterOrder[Math.Max(0, index - 1)]);
    }

    private void OnNext(UiElementId sender, RoutedEventArgs args)
    {
        int index = ChapterIndex(currentChapter);
        ShowChapter(ChapterOrder[index == ChapterOrder.Length - 1 ? 0 : index + 1]);
    }

    private void ShowChapter(PresentationChapter chapter)
    {
        if (currentChapter == PresentationChapter.Prism && chapter != PresentationChapter.Prism)
        {
            PagePrism.Deactivate();
        }
        if (currentChapter == PresentationChapter.Aspect && chapter != PresentationChapter.Aspect)
        {
            PageAspect.Deactivate();
        }

        currentChapter = chapter;
        ChapterScrollViewer.Visibility = chapter is PresentationChapter.Aspect or PresentationChapter.Prism
            ? Visibility.Collapsed
            : Visibility.Visible;
        foreach (PresentationChapter candidate in ChapterOrder)
        {
            bool selected = candidate == currentChapter;
            tourPages[candidate].Visibility = selected
                ? Visibility.Visible
                : candidate == PresentationChapter.Aspect &&
                  (aspectPagePrewarming || aspectPagePreparedForNavigation)
                    ? Visibility.Hidden
                    : Visibility.Collapsed;
            tourNavigation[candidate].IsChecked = selected;
        }
        if (chapter is not PresentationChapter.Aspect and not PresentationChapter.Prism)
        {
            ChapterScrollViewer.ScrollInfo.SetVerticalOffset(0);
        }

        if (currentChapter == PresentationChapter.Aspect)
        {
            PageAspect.Activate();
        }
        if (currentChapter == PresentationChapter.Prism)
        {
            PagePrism.Activate();
        }

        int index = ChapterIndex(currentChapter);
        HeaderChapterText.Text = ChapterName(currentChapter);
        ChapterCounter.Text = $"CHAPTER {index + 1:00} / {ChapterOrder.Length:00}";
        PreviousButton.IsEnabled = index > 0;
        NextButton.Content = index == ChapterOrder.Length - 1 ? "RESTART TOUR  ->" : "NEXT  ->";

    }

    private static int ChapterIndex(PresentationChapter chapter) => Array.IndexOf(ChapterOrder, chapter);

    private static string ChapterName(PresentationChapter chapter) => chapter switch
    {
        PresentationChapter.Welcome => "WELCOME",
        PresentationChapter.RetainedModel => "RETAINED MODEL",
        PresentationChapter.Markup => "BUILD-TIME MARKUP",
        PresentationChapter.Aspect => "ASPECT STUDIO",
        PresentationChapter.Motion => "MOTION",
        PresentationChapter.Prism => "PRISM",
        PresentationChapter.FramePipeline => "FRAME PIPELINE",
        _ => throw new ArgumentOutOfRangeException(nameof(chapter), chapter, "Unknown presentation chapter.")
    };
    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TOUR_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath = Path.GetFullPath(path);
        string errorPath = fullPath + ".error.txt";
        bool closeAfterCapture = string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CLOSE_AFTER_CAPTURE"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        File.Delete(errorPath);
        try
        {
            PresentationChapter captureChapter = currentChapter;
            if (captureChapter == PresentationChapter.Prism && string.Equals(
                    Environment.GetEnvironmentVariable("CERNEALA_PRISM_CONFORMANCE_PRESET"),
                    "1",
                    StringComparison.Ordinal))
            {
                conformancePrismSamples = PagePrism.ConfigureConformanceScene();
            }
            if (int.TryParse(
                    Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_HOVER_CHAPTER"),
                    out int hoverChapter) &&
                hoverChapter >= 1 &&
                hoverChapter <= ChapterOrder.Length)
            {
                tourNavigation[ChapterOrder[hoverChapter - 1]].IsPointerOver = true;
            }

            bool captureDuringMotion = string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION"),
                "1",
                StringComparison.OrdinalIgnoreCase);
            if (captureDuringMotion && !closeAfterCapture)
            {
                await Task.Delay(1_350);
            }
            else if (!closeAfterCapture)
            {
                await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(100);
            }

            int captureIndex = ChapterIndex(captureChapter);
            PresentationChapter previousChapter = ChapterOrder[
                (captureIndex - 1 + ChapterOrder.Length) % ChapterOrder.Length];
            ShowChapter(previousChapter);
            ButtonAutomationPeer next = new(NextButton);
            bool settledCapture = string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_SETTLED_CAPTURE"),
                "1",
                StringComparison.OrdinalIgnoreCase);
            suppressLiveDiagnostics = settledCapture || captureChapter != PresentationChapter.Aspect;
            if (suppressLiveDiagnostics)
            {
                SetConformanceHeaderDiagnostics();
            }
            try
            {
                if (settledCapture)
                {
                    if (!next.Invoke())
                    {
                        throw new InvalidOperationException("Presentation capture could not navigate to its target chapter.");
                    }

                    await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
                    await Task.Delay(100);
                    SaveScreenshot(fullPath);
                }
                else
                {
                    await CaptureScreenshotFrameAsync(fullPath, () =>
                    {
                        if (!next.Invoke())
                        {
                            throw new InvalidOperationException("Presentation capture could not navigate to its target chapter.");
                        }
                    });
                }
            }
            finally
            {
                suppressLiveDiagnostics = false;
            }
            await File.WriteAllLinesAsync(Path.ChangeExtension(fullPath, ".metrics.txt"),
            [
                $"Chapter={ChapterIndex(currentChapter) + 1}",
                $"RootCommands={Root?.RetainedRenderCache.RootCommands.Count ?? 0}",
                $"RenderCacheVersion={Root?.RetainedRenderCache.Version ?? 0}",
                .. conformancePrismSamples.Select(sample => $"PrismSample={sample}")
            ]);
            if (closeAfterCapture)
            {
                Close();
            }
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
        }
    }

    private void SetConformanceHeaderDiagnostics()
    {
        HeaderDiagFrame.Text = "FIXED FRAME\nWORK COMMITTED";
        HeaderDiagPhases.Text = "INHERITED 0  COMMAND 0\nASPECT 0";
        HeaderDiagLayout.Text = "QUEUED 0 / 0\nCALLS 0 / 0";
        HeaderDiagRender.Text = "RENDER 1  HIT 0\nREUSED 0  NO-WORK 0";
        HeaderDiagMotion.Text = "FRAME 0  SAMPLE 0  VALUE 0  WRITE 0\nDONE 0  R-INV 0  L-INV 0  REDUCED 0";
        HeaderDiagRelay.Text = "SNAP 1  DEQ 1  EXEC 1  BACK 0\nCANCEL 0  FAULT 0  DEFER 0";
    }

    private async Task CaptureScreenshotFrameAsync(string fullPath, Action frameTrigger)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int renderedFrames = 0;
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            renderedFrames++;
            if (renderedFrames < 4 || Root?.RetainedRenderCache.IsRootValid != true)
            {
                Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot settle");
                return;
            }

            FrameRendered -= handler;
            try
            {
                SaveScreenshot(fullPath);
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        };

        FrameRendered += handler;
        frameTrigger();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        while (!completion.Task.IsCompleted)
        {
            timeout.Token.ThrowIfCancellationRequested();
            Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
            await Task.WhenAny(completion.Task, Task.Delay(16, timeout.Token));
        }
        await completion.Task;
    }
}
